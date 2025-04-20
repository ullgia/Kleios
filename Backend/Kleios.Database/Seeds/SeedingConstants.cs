namespace Kleios.Database.Seeds;

/// <summary>
/// Classe contenente costanti utilizzate per il seeding del database
/// </summary>
public static class SeedingConstants
{
    // Utenti predefiniti
    public static class Users
    {
        // Utente Master
        public static class Master
        {
            public const string Username = "master";
            public const string Email = "master@kleios.com";
            public const string Password = "master123";
            public const string FirstName = "Master";
            public const string LastName = "User";
        }
        
        // Utente normale
        public static class Regular
        {
            public const string Username = "user";
            public const string Email = "user@kleios.com";
            public const string Password = "user123";
            public const string FirstName = "Regular";
            public const string LastName = "User";
        }
    }
    
    // Ruoli predefiniti
    public static class Roles
    {
        public const string Administrator = "Amministratore";
        public const string User = "Utente";
    }
    
    // Nomi dei permessi di sistema
    public static class Permissions
    {
        // Permessi relativi ai logs
        public static class Logs
        {
            public const string View = "Logs.View";
            public const string ViewName = "Visualizza Logs";
            public const string ViewDescription = "Permette di visualizzare i logs del sistema";
            
            public const string Manage = "Logs.Manage";
            public const string ManageName = "Gestisci Logs";
            public const string ManageDescription = "Permette di gestire i logs del sistema";
        }
        
        // Permessi relativi agli utenti
        public static class Users
        {
            public const string View = "Users.View";
            public const string ViewName = "Visualizza Utenti";
            public const string ViewDescription = "Permette di visualizzare gli utenti del sistema";
            
            public const string Manage = "Users.Manage";
            public const string ManageName = "Gestisci Utenti";
            public const string ManageDescription = "Permette di gestire gli utenti del sistema";
        }
        
        // Permessi relativi alle impostazioni
        public static class Settings
        {
            public const string View = "Settings.View";
            public const string ViewName = "Visualizza Impostazioni";
            public const string ViewDescription = "Permette di visualizzare le impostazioni del sistema";
            
            public const string Manage = "Settings.Manage";
            public const string ManageName = "Gestisci Impostazioni";
            public const string ManageDescription = "Permette di gestire le impostazioni del sistema";
        }
    }
}