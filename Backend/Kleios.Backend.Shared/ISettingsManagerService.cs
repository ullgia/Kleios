using Kleios.Shared.Settings;

namespace Kleios.Backend.Shared;

/// <summary>
/// Interfaccia per il servizio di gestione delle impostazioni dell'applicazione
/// </summary>
public interface ISettingsManagerService : ISettingsManager
{
    /// <summary>
    /// Inizializza il servizio caricando le impostazioni dal database
    /// </summary>
    Task InitializeAsync();
    
    /// <summary>
    /// Salva l'intero modello delle impostazioni nel database
    /// </summary>
    Task<bool> SaveSettingsAsync(AppSettingsModel settings);
    
    /// <summary>
    /// Salva una sezione specifica di impostazioni
    /// </summary>
    Task<bool> SaveSectionAsync<T>(string sectionName, T section) where T : class;
    
    /// <summary>
    /// Aggiorna un'impostazione specifica
    /// </summary>
    Task<bool> UpdateSettingAsync(string settingName, string value);
}