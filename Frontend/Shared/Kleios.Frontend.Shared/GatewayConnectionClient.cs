using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Kleios.Frontend.Shared;

/// <summary>
/// Client per la connessione e registrazione con il Gateway
/// </summary>
public class GatewayConnectionClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GatewayConnectionClient> _logger;
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _heartbeatCts;
    private bool _disposed;

    public GatewayConnectionClient(HttpClient httpClient, ILogger<GatewayConnectionClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registra il servizio presso il Gateway
    /// </summary>
    public async Task<bool> RegisterAsync(ServiceRegistration registration, CancellationToken cancellationToken = default)
    {
        if (registration == null)
            throw new ArgumentNullException(nameof(registration));

        var retryCount = 0;
        var maxRetries = KleiosConstants.WebSocket.MaxRetryAttempts;
        var delay = TimeSpan.FromSeconds(2);

        while (retryCount < maxRetries)
        {
            try
            {
                _logger.LogInformation(
                    "Tentativo {Attempt}/{MaxAttempts} di registrazione servizio '{ServiceName}' al Gateway...",
                    retryCount + 1, maxRetries, registration.ServiceName);

                var json = JsonSerializer.Serialize(registration);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    KleiosConstants.GatewayRegistrationEndpoint,
                    content,
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Servizio '{ServiceName}' registrato con successo al Gateway. Prefix: '{Prefix}'",
                        registration.ServiceName, registration.RoutePrefix);
                    return true;
                }

                _logger.LogWarning(
                    "Registrazione fallita con status code {StatusCode}. Retry in {Delay}s...",
                    response.StatusCode, delay.TotalSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Errore durante registrazione servizio '{ServiceName}' (tentativo {Attempt}/{MaxAttempts})",
                    registration.ServiceName, retryCount + 1, maxRetries);
            }

            retryCount++;
            if (retryCount < maxRetries)
            {
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 30)); // Exponential backoff, max 30s
            }
        }

        _logger.LogError(
            "Impossibile registrare il servizio '{ServiceName}' dopo {MaxRetries} tentativi",
            registration.ServiceName, maxRetries);
        return false;
    }

    /// <summary>
    /// Connette al Gateway via WebSocket per heartbeat
    /// </summary>
    public async Task<bool> ConnectWebSocketAsync(string serviceName, string gatewayUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentNullException(nameof(serviceName));
        if (string.IsNullOrWhiteSpace(gatewayUrl))
            throw new ArgumentNullException(nameof(gatewayUrl));

        try
        {
            _webSocket?.Dispose();
            _webSocket = new ClientWebSocket();

            var wsUrl = gatewayUrl.Replace("https://", "wss://").Replace("http://", "ws://");
            var endpoint = KleiosConstants.GatewayWebSocketEndpoint.Replace("{serviceName}", serviceName);
            var uri = new Uri($"{wsUrl}{endpoint}");

            _logger.LogInformation("Connessione WebSocket al Gateway: {Uri}", uri);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(KleiosConstants.WebSocket.ConnectionTimeoutSeconds));

            await _webSocket.ConnectAsync(uri, cts.Token);

            _logger.LogInformation("WebSocket connesso con successo");

            // Avvia heartbeat in background
            StartHeartbeat(serviceName, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante connessione WebSocket");
            return false;
        }
    }

    /// <summary>
    /// Invia heartbeat periodici al Gateway
    /// </summary>
    private void StartHeartbeat(string serviceName, CancellationToken cancellationToken)
    {
        _heartbeatCts?.Cancel();
        _heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _ = Task.Run(async () =>
        {
            var interval = TimeSpan.FromSeconds(KleiosConstants.WebSocket.HeartbeatIntervalSeconds);

            while (!_heartbeatCts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(interval, _heartbeatCts.Token);

                    if (_webSocket?.State == WebSocketState.Open)
                    {
                        await SendHeartbeatAsync(serviceName, _heartbeatCts.Token);
                    }
                    else
                    {
                        _logger.LogWarning("WebSocket non connesso. Tentativo riconnessione...");
                        // Qui si potrebbe implementare la riconnessione automatica
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    // Normale cancellazione
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Errore durante invio heartbeat");
                }
            }
        }, _heartbeatCts.Token);
    }

    /// <summary>
    /// Invia un singolo heartbeat
    /// </summary>
    public async Task SendHeartbeatAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        if (_webSocket?.State != WebSocketState.Open)
        {
            _logger.LogWarning("WebSocket non connesso, impossibile inviare heartbeat");
            return;
        }

        try
        {
            var message = JsonSerializer.Serialize(new
            {
                type = "heartbeat",
                serviceName,
                timestamp = DateTime.UtcNow
            });

            var bytes = Encoding.UTF8.GetBytes(message);
            var buffer = new ArraySegment<byte>(bytes);

            await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);

            _logger.LogDebug("Heartbeat inviato per servizio '{ServiceName}'", serviceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante invio heartbeat per '{ServiceName}'", serviceName);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _heartbeatCts?.Cancel();
        _heartbeatCts?.Dispose();
        _webSocket?.Dispose();

        _disposed = true;
    }
}
