namespace Kleios.Frontend.Shared;

/// <summary>
/// Costanti condivise tra tutti i moduli Kleios
/// </summary>
public static class KleiosConstants
{
    /// <summary>
    /// Nome del cookie per JWT authentication
    /// </summary>
    public const string AuthCookieName = "Kleios.AuthToken";

    /// <summary>
    /// Endpoint del Gateway per registrazione servizi
    /// </summary>
    public const string GatewayRegistrationEndpoint = "/api/_gateway/register";

    /// <summary>
    /// Endpoint del Gateway per lista routes
    /// </summary>
    public const string GatewayRoutesEndpoint = "/api/_gateway/routes";

    /// <summary>
    /// Endpoint del Gateway per lista servizi (admin)
    /// </summary>
    public const string GatewayServicesEndpoint = "/api/_gateway/services";

    /// <summary>
    /// WebSocket endpoint del Gateway per heartbeat
    /// </summary>
    public const string GatewayWebSocketEndpoint = "/ws/{serviceName}";

    /// <summary>
    /// Health check endpoint standard per tutti i moduli
    /// </summary>
    public const string HealthCheckEndpoint = "/_health";

    /// <summary>
    /// Paths per authentication
    /// </summary>
    public static class AuthPaths
    {
        public const string Login = "/auth/Account/Login";
        public const string Logout = "/auth/Account/Logout";
        public const string Register = "/auth/Account/Register";
        public const string ForgotPassword = "/auth/Account/ForgotPassword";
        public const string AccessDenied = "/auth/Account/AccessDenied";
    }

    /// <summary>
    /// Configurazione WebSocket
    /// </summary>
    public static class WebSocket
    {
        /// <summary>
        /// Intervallo heartbeat in secondi
        /// </summary>
        public const int HeartbeatIntervalSeconds = 30;

        /// <summary>
        /// Timeout per connessione WebSocket
        /// </summary>
        public const int ConnectionTimeoutSeconds = 10;

        /// <summary>
        /// Numero massimo retry per connessione
        /// </summary>
        public const int MaxRetryAttempts = 5;
    }

    /// <summary>
    /// Configurazione Health Check
    /// </summary>
    public static class HealthCheck
    {
        /// <summary>
        /// Timeout per health check request
        /// </summary>
        public const int TimeoutSeconds = 5;

        /// <summary>
        /// Numero massimo di failed checks prima di deregistrare
        /// </summary>
        public const int MaxFailedChecks = 3;

        /// <summary>
        /// Intervallo check in secondi
        /// </summary>
        public const int IntervalSeconds = 30;
    }
}
