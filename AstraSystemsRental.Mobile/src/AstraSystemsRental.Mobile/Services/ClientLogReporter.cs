using System.Net.Http.Json;
using System.Text.Json;

namespace AstraSystemsRental.Mobile.Services;

public interface IClientLogReporter
{
    void ReportApiFailure(string method, string path, int statusCode, string? error);
    void ReportException(Exception exception, string context);
}

/// <summary>
/// Envia a la API los fallos que ocurren en el dispositivo, para que aparezcan
/// en la misma vista de Logs de la web. Sin esto los errores de la app son
/// invisibles: el servidor solo registra lo que falla de su lado, asi que un
/// bug de cliente sobre una respuesta 200 no deja rastro en ningun lado.
///
/// Todo el envio es "fire and forget" y traga sus propias excepciones: un fallo
/// al reportar un fallo no puede romper la pantalla que el usuario esta viendo.
/// </summary>
public sealed class ClientLogReporter : IClientLogReporter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private const string Path = "/apiUsers/logs/client";

    private readonly HttpClient _http;
    private readonly ISessionStore _session;

    public ClientLogReporter(HttpClient http, ISessionStore session)
    {
        _http = http;
        _session = session;
    }

    public void ReportApiFailure(string method, string path, int statusCode, string? error)
    {
        // 401 y 0 se excluyen: el primero es el flujo normal de refresco de token
        // y el segundo es falta de red, que ya se maneja con la cola offline.
        // Registrarlos llenaria la tabla de ruido sin senal.
        if (statusCode is 0 or 401 || path.Contains("/logs/client", StringComparison.Ordinal))
            return;

        Send(new
        {
            level = statusCode >= 500 ? "Error" : "Warning",
            platform = DeviceInfo.Current.Platform.ToString(),
            message = string.IsNullOrWhiteSpace(error) ? $"{method} {path} respondió {statusCode}" : error,
            requestMethod = method,
            requestPath = path,
            statusCode
        });
    }

    public void ReportException(Exception exception, string context)
        => Send(new
        {
            level = "Error",
            platform = DeviceInfo.Current.Platform.ToString(),
            message = $"{context}: {exception.Message}",
            exceptionType = exception.GetType().Name,
            exceptionDetail = exception.ToString()
        });

    private void Send(object payload)
    {
        // Sin sesion no hay a quien atribuir el log y el endpoint exige token.
        if (string.IsNullOrWhiteSpace(_session.Current?.AccessToken))
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, Path)
                {
                    Content = JsonContent.Create(payload, options: JsonOptions)
                };

                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _session.Current!.AccessToken);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                using var response = await _http.SendAsync(request, cts.Token);
            }
            catch
            {
                // Deliberado: reportar es best-effort. Si no hay red, el fallo se
                // pierde antes que arriesgar un bucle de errores al reportar.
            }
        });
    }
}
