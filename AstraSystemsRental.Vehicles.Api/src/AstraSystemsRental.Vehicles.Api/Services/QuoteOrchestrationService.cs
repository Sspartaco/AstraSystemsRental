using System.Threading.Channels;
using AstraSystemsRental.Vehicles.Api.Configuration;
using AstraSystemsRental.Vehicles.Api.Domain;
using AstraSystemsRental.Vehicles.Api.Persistence;
using Microsoft.Extensions.Options;

namespace AstraSystemsRental.Vehicles.Api.Services;

public sealed record QuoteFetchJob(long QuoteRequestId, VehicleQuery Query);

public sealed class QuoteOrchestrationService : BackgroundService, IQuoteOrchestrator
{
    private readonly Channel<QuoteFetchJob> _channel = Channel.CreateUnbounded<QuoteFetchJob>();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ValuationOptions _options;
    private readonly ILogger<QuoteOrchestrationService> _logger;

    public QuoteOrchestrationService(
        IServiceScopeFactory scopeFactory,
        IOptions<ValuationOptions> options,
        ILogger<QuoteOrchestrationService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    public void Enqueue(long quoteRequestId, VehicleQuery query)
        => _channel.Writer.TryWrite(new QuoteFetchJob(quoteRequestId, query));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workerCount = Math.Max(1, _options.MaxConcurrentFetches);
        var workers = Enumerable.Range(0, workerCount).Select(_ => RunWorkerAsync(stoppingToken));
        var purgeLoop = RunPurgeLoopAsync(stoppingToken);

        await Task.WhenAll(workers.Append(purgeLoop));
    }

    private async Task RunWorkerAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessJobAsync(job, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Unhandled error processing quote job for request {RequestId}", job.QuoteRequestId);
            }
        }
    }

    private async Task ProcessJobAsync(QuoteFetchJob job, CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var quoteRepository = scope.ServiceProvider.GetRequiredService<IQuoteRepository>();
        var sources = scope.ServiceProvider.GetServices<IValuationSource>().ToList();

        var activeSources = await quoteRepository.GetActiveSourcesAsync(stoppingToken);
        var activeCodes = activeSources.Select(s => s.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var applicableSources = sources.Where(s => activeCodes.Contains(s.SourceCode)).ToList();
        var anyFailure = false;

        foreach (var source in applicableSources)
        {
            var cached = await quoteRepository.GetCacheEntryAsync(job.Query.PlateNumber, source.SourceCode, stoppingToken);
            if (cached is { ExpiresAtUtc: { } expires } && expires > DateTime.UtcNow && cached.Status == ValuationStatus.Success)
                continue;

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(1, _options.SourceTimeoutSeconds)));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeoutCts.Token);

            ValuationSourceResult result;
            try
            {
                result = await source.FetchAsync(job.Query, linkedCts.Token);
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                result = new ValuationSourceResult(ValuationStatus.Failed, null, null, null, null, "Tiempo de espera agotado.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Source {Source} failed for plate {Plate}", source.SourceCode, job.Query.PlateNumber);
                result = new ValuationSourceResult(ValuationStatus.Failed, null, null, null, null, ex.Message);
            }

            if (result.Status == ValuationStatus.Failed)
                anyFailure = true;

            var ttl = result.Status == ValuationStatus.Success
                ? TimeSpan.FromDays(_options.CacheTtlDays)
                : TimeSpan.FromMinutes(_options.FailedCacheTtlMinutes);

            await quoteRepository.UpsertCacheEntryAsync(new ValuationCacheEntry
            {
                PlateNumber = job.Query.PlateNumber,
                SourceCode = source.SourceCode,
                Status = result.Status,
                ValueMin = result.ValueMin,
                ValueMax = result.ValueMax,
                ValueAvg = result.ValueAvg,
                RawPayload = result.RawPayload,
                ErrorMessage = result.ErrorMessage,
                FetchedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.Add(ttl)
            }, stoppingToken);
        }

        await quoteRepository.UpdateQuoteRequestStatusAsync(
            job.QuoteRequestId,
            anyFailure ? QuoteRequestStatus.CompletedWithErrors : QuoteRequestStatus.Completed,
            DateTime.UtcNow,
            stoppingToken);
    }

    private async Task RunPurgeLoopAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();
            var quoteRepository = scope.ServiceProvider.GetRequiredService<IQuoteRepository>();
            var cutoff = DateTime.UtcNow.AddHours(-_options.PurgeCompletedRequestsAfterHours);
            var purged = await quoteRepository.PurgeCompletedRequestsOlderThanAsync(cutoff, stoppingToken);
            if (purged > 0)
                _logger.LogInformation("Purged {Count} stale quote requests", purged);
        }
    }
}
