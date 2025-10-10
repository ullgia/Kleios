using System.Net.WebSockets;

namespace Kleios.Gateway.Services;

/// <summary>
/// Manager per gestire le connessioni WebSocket con i moduli
/// </summary>
public interface IWebSocketManager
{
    /// <summary>
    /// Gestisce una connessione WebSocket per un servizio
    /// </summary>
    Task HandleWebSocketAsync(WebSocket webSocket, string serviceName, CancellationToken cancellationToken);

    /// <summary>
    /// Invia un messaggio a un servizio specifico
    /// </summary>
    Task SendToServiceAsync(string serviceName, string message, CancellationToken cancellationToken);

    /// <summary>
    /// Invia un messaggio broadcast a tutti i servizi connessi
    /// </summary>
    Task BroadcastAsync(string message, CancellationToken cancellationToken);

    /// <summary>
    /// Ottiene il numero di connessioni WebSocket attive
    /// </summary>
    int GetActiveConnectionsCount();
}
