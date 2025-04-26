using Kleios.Shared.Settings;

namespace Kleios.Backend.Shared;

/// <summary>
/// Interfaccia per il gestore delle impostazioni dell'applicazione
/// </summary>
public interface ISettingsManager
{
    /// <summary>
    /// Ottiene tutte le impostazioni come modello strutturato
    /// </summary>
    AppSettingsModel GetSettings();

    /// <summary>
    /// Ottiene una sezione specifica di impostazioni
    /// </summary>
    T GetSection<T>(string sectionName) where T : class, new();
    
    /// <summary>
    /// Ottiene un valore di impostazione specifico in base al nome (es. "Jwt:Secret")
    /// </summary>
    string GetValue(string settingName, string defaultValue = "");
    
    /// <summary>
    /// Ottiene un valore di impostazione come intero
    /// </summary>
    int GetIntValue(string settingName, int defaultValue = 0);
    
    /// <summary>
    /// Ottiene un valore di impostazione come booleano
    /// </summary>
    bool GetBoolValue(string settingName, bool defaultValue = false);
    
    /// <summary>
    /// Ottiene un valore di impostazione come double
    /// </summary>
    double GetDoubleValue(string settingName, double defaultValue = 0);
    
    /// <summary>
    /// Ottiene un valore di impostazione come DateTime
    /// </summary>
    DateTime GetDateTimeValue(string settingName, DateTime? defaultValue = null);
    
    /// <summary>
    /// Ottiene i metadati di tutte le impostazioni
    /// </summary>
    IEnumerable<SettingMetadata> GetAllSettingMetadata();
    
    /// <summary>
    /// Ottiene i metadati di un gruppo specifico di impostazioni
    /// </summary>
    IEnumerable<SettingMetadata> GetSettingMetadataByGroup(string group);
    
    /// <summary>
    /// Ottiene tutti i gruppi di impostazioni
    /// </summary>
    IEnumerable<SettingGroupMetadata> GetAllGroups();
}