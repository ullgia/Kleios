namespace Kleios.Shared.Authorization;

/// <summary>
/// Definizione centralizzata di tutti i permessi dell'applicazione,
/// condivisa tra frontend e backend
/// </summary>
public static class AppPermissions
{
    /// <summary>
    /// Permessi relativi ai logs di sistema
    /// </summary>
    public static class Logs
    {
        /// <summary>
        /// Permesso per visualizzare i logs
        /// </summary>
        [Permission("Visualizza Logs", "Permette di visualizzare i logs del sistema")]
        public const string View = "Logs.View";
        
        /// <summary>
        /// Permesso per gestire i logs
        /// </summary>
        [Permission("Gestisci Logs", "Permette di gestire i logs del sistema")]
        public const string Manage = "Logs.Manage";
    }
    
    /// <summary>
    /// Permessi relativi agli utenti
    /// </summary>
    public static class Users
    {
        /// <summary>
        /// Permesso per visualizzare gli utenti
        /// </summary>
        [Permission("Visualizza Utenti", "Permette di visualizzare gli utenti del sistema")]
        public const string View = "Users.View";
        
        /// <summary>
        /// Permesso per gestire gli utenti
        /// </summary>
        [Permission("Gestisci Utenti", "Permette di gestire gli utenti del sistema")]
        public const string Manage = "Users.Manage";
    }
    
    /// <summary>
    /// Permessi relativi alle impostazioni
    /// </summary>
    public static class Settings
    {
        /// <summary>
        /// Permesso per visualizzare le impostazioni
        /// </summary>
        [Permission("Visualizza Impostazioni", "Permette di visualizzare le impostazioni del sistema")]
        public const string View = "Settings.View";
        
        /// <summary>
        /// Permesso per gestire le impostazioni
        /// </summary>
        [Permission("Gestisci Impostazioni", "Permette di gestire le impostazioni del sistema")]
        public const string Manage = "Settings.Manage";
    }
}