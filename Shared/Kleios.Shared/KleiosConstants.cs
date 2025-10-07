namespace Kleios.Shared;

/// <summary>
/// Costanti globali per l'applicazione Kleios
/// </summary>
public static class KleiosConstants
{
    /// <summary>
    /// Costanti per l'autenticazione
    /// </summary>
    public static class Authentication
    {
        /// <summary>
        /// Nome del cookie di autenticazione
        /// </summary>
        public const string CookieName = "Kleios.AuthToken";
        
        /// <summary>
        /// Durata del cookie in minuti (sliding expiration)
        /// </summary>
        public const int CookieExpirationMinutes = 60;
        
        /// <summary>
        /// Threshold in secondi prima della scadenza del token per il refresh automatico
        /// </summary>
        public const int TokenExpiryThresholdSeconds = 30;
    }
    
    /// <summary>
    /// Costanti per il rate limiting
    /// </summary>
    public static class RateLimiting
    {
        /// <summary>
        /// Rate limit per gli endpoint di autenticazione (login/refresh)
        /// </summary>
        public const int AuthenticationPermitLimit = 10;
        
        /// <summary>
        /// Rate limit per le chiamate API generiche
        /// </summary>
        public const int ApiPermitLimit = 100;
        
        /// <summary>
        /// Rate limit per le pagine frontend
        /// </summary>
        public const int DefaultPermitLimit = 200;
        
        /// <summary>
        /// Finestra temporale per il rate limiting (minuti)
        /// </summary>
        public const int WindowMinutes = 1;
    }
    
    /// <summary>
    /// Costanti per i servizi (service discovery)
    /// </summary>
    public static class Services
    {
        public const string AuthBackend = "auth-backend";
        public const string SystemBackend = "system-backend";
        public const string AuthFrontend = "auth-module";
        public const string SystemFrontend = "system-module";
        public const string Shell = "shell";
        public const string Gateway = "gateway";
    }
    
    /// <summary>
    /// Costanti per le porte (solo gateway è fissa)
    /// </summary>
    public static class Ports
    {
        /// <summary>
        /// Porta fissa del gateway (entry point unico)
        /// </summary>
        public const int Gateway = 5000;
    }
}
