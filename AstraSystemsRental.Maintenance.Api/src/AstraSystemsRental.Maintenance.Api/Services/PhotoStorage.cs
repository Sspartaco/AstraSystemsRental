namespace AstraSystemsRental.Maintenance.Api.Services;

public sealed record StoredPhoto(string FileName, string StoragePath);

public interface IPhotoStorage
{
    Task<StoredPhoto?> SaveAsync(long reservationId, IFormFile file, CancellationToken cancellationToken);
    Stream? OpenRead(string storagePath, out string contentType);
}

public sealed class LocalPhotoStorage(IWebHostEnvironment environment, ILogger<LocalPhotoStorage> logger) : IPhotoStorage
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxSizeBytes = 8 * 1024 * 1024;

    public async Task<StoredPhoto?> SaveAsync(long reservationId, IFormFile file, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension) || file.Length > MaxSizeBytes)
            return null;

        var relativeDirectory = Path.Combine("uploads", "workshop-reservations", reservationId.ToString());
        var root = string.IsNullOrWhiteSpace(environment.ContentRootPath) ? Directory.GetCurrentDirectory() : environment.ContentRootPath;
        var absoluteDirectory = Path.Combine(root, relativeDirectory);

        Directory.CreateDirectory(absoluteDirectory);

        var safeName = $"{Guid.NewGuid():N}{extension}";
        var absolutePath = Path.Combine(absoluteDirectory, safeName);

        await using (var stream = File.Create(absolutePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        logger.LogInformation("Stored reservation photo {FileName} for reservation {ReservationId}", safeName, reservationId);

        return new StoredPhoto(file.FileName, Path.Combine(relativeDirectory, safeName).Replace('\\', '/'));
    }

    public Stream? OpenRead(string storagePath, out string contentType)
    {
        contentType = "application/octet-stream";

        if (string.IsNullOrWhiteSpace(storagePath))
            return null;

        var root = string.IsNullOrWhiteSpace(environment.ContentRootPath) ? Directory.GetCurrentDirectory() : environment.ContentRootPath;
        var uploadsRoot = Path.GetFullPath(Path.Combine(root, "uploads"));
        var absolutePath = Path.GetFullPath(Path.Combine(root, storagePath.Replace('/', Path.DirectorySeparatorChar)));

        if (!absolutePath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(absolutePath))
            return null;

        var extension = Path.GetExtension(absolutePath).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension))
            return null;

        contentType = extension switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

        return File.OpenRead(absolutePath);
    }
}
