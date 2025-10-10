using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace Kleios.Gateway.Services;

/// <summary>
/// Implementazione del WebSocket manager per gestire connessioni heartbeat con i moduli
/// </summary>
public class WebSocketManager : IWebSocketManager
{
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();
    private readonly IServiceRegistry _serviceRegistry;
    private readonly ILogger<WebSocketManager> _logger;

    public WebSocketManager(
        IServiceRegistry serviceRegistry,
        ILogger<WebSocketManager> logger)
    {
        _serviceRegistry = serviceRegistry;
        _logger = logger;
    }

    public async Task HandleWebSocketAsync(WebSocket webSocket, string serviceName, CancellationToken cancellationToken)
    {
        // Aggiungi o aggiorna la connessione
        _connections.AddOrUpdate(serviceName, webSocket, (key, oldSocket) =>
        {
            // Chiudi la vecchia connessione se presente
            if (oldSocket.State == WebSocketState.Open)
            {
                _ = oldSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "New connection established", CancellationToken.None);
            }
            return webSocket;
        });

        _logger.LogInformation("WebSocket connesso per servizio {ServiceName}", serviceName);

        try
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result;

            do
            {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logger.LogDebug("Messaggio ricevuto da {ServiceName}: {Message}", serviceName, message);

                    // Risponde con pong ai ping (heartbeat)
                    if (message.Contains("\"type\":\"ping\"", StringComparison.OrdinalIgnoreCase))
                    {
                        var pongMessage = "{\"type\":\"pong\",\"timestamp\":\"" + DateTime.UtcNow.ToString("o") + "\"}";
                        await SendMessageAsync(webSocket, pongMessage, cancellationToken);
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("WebSocket chiuso per servizio {ServiceName}", serviceName);
                    break;
                }

            } while (!result.CloseStatus.HasValue && !cancellationToken.IsCancellationRequested);

            // Chiudi la connessione
            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(
                    result.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                    result.CloseStatusDescription ?? "Connection closed",
                    cancellationToken);
            }
        }
        catch (WebSocketException ex)
        {
            _logger.LogWarning(ex, "WebSocket error per servizio {ServiceName}", serviceName);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("WebSocket operazione cancellata per servizio {ServiceName}", serviceName);
        }
        finally
        {
            // Rimuovi la connessione
            _connections.TryRemove(serviceName, out _);
            
            // IMPORTANTE: Deregistra il servizio quando perde la connessione WebSocket
            _logger.LogWarning("WebSocket disconnesso per servizio {ServiceName}. Deregistrazione in corso...", serviceName);
            await _serviceRegistry.UnregisterServiceAsync(serviceName);

            webSocket.Dispose();
        }
    }

    public async Task SendToServiceAsync(string serviceName, string message, CancellationToken cancellationToken)
    {
        if (_connections.TryGetValue(serviceName, out var webSocket))
        {
            if (webSocket.State == WebSocketState.Open)
            {
                await SendMessageAsync(webSocket, message, cancellationToken);
            }
            else
            {
                _logger.LogWarning("WebSocket per {ServiceName} non è in stato Open", serviceName);
            }
        }
        else
        {
            _logger.LogWarning("Nessuna connessione WebSocket trovata per {ServiceName}", serviceName);
        }
    }

    public async Task BroadcastAsync(string message, CancellationToken cancellationToken)
    {
        var tasks = _connections
            .Where(kvp => kvp.Value.State == WebSocketState.Open)
            .Select(kvp => SendMessageAsync(kvp.Value, message, cancellationToken));

        await Task.WhenAll(tasks);
        
        _logger.LogDebug("Messaggio broadcast inviato a {Count} servizi", _connections.Count);
    }

    public int GetActiveConnectionsCount()
    {
        return _connections.Count(kvp => kvp.Value.State == WebSocketState.Open);
    }

    private async Task SendMessageAsync(WebSocket webSocket, string message, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        await webSocket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            endOfMessage: true,
            cancellationToken);
    }
}
