using Kleios.Shared;
using Kleios.Shared.Settings;

namespace Kleios.Backend.Shared;

/// <summary>
/// DTO per la creazione/aggiornamento di un'impostazione
/// </summary>
public class SettingDto
{
    public required string Key { get; set; }
    public required string Value { get; set; }
    public required string Description { get; set; }
    public required string DataType { get; set; }
    public bool IsRequired { get; set; }
    public bool IsReadOnly { get; set; }
    public required string Category { get; set; }
}

/// <summary>
/// Interfaccia per il servizio di gestione delle impostazioni a livello di database
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Ottiene tutte le impostazioni dal database
    /// </summary>
    Task<Option<IEnumerable<SettingMetadata>>> GetAllSettingsAsync();
    
    /// <summary>
    /// Ottiene le impostazioni di una categoria specifica
    /// </summary>
    Task<Option<IEnumerable<SettingMetadata>>> GetSettingsByCategoryAsync(string category);
    
    /// <summary>
    /// Ottiene un'impostazione specifica per chiave
    /// </summary>
    Task<Option<SettingMetadata>> GetSettingByKeyAsync(string key);
    
    /// <summary>
    /// Aggiorna un'impostazione
    /// </summary>
    Task<Option<SettingMetadata>> UpdateSettingAsync(string key, string value);
    
    /// <summary>
    /// Crea una nuova impostazione usando un DTO
    /// </summary>
    Task<Option<SettingMetadata>> CreateSettingAsync(SettingDto dto);
    
    /// <summary>
    /// Crea una nuova impostazione
    /// </summary>
    Task<Option<SettingMetadata>> CreateSettingAsync(string key, string value, string description, string dataType, bool isRequired, bool isReadOnly, string category);
    
    /// <summary>
    /// Elimina un'impostazione
    /// </summary>
    Task<Option> DeleteSettingAsync(string key);
}