namespace Kleios.Shared.Settings;

/// <summary>
/// Rappresenta i metadati di un'impostazione
/// </summary>
public class SettingMetadata
{
    /// <summary>
    /// Identificatore univoco dell'impostazione
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Nome dell'impostazione (es. "Jwt:Secret")
    /// </summary>
    public required string Key { get; set; } 
    
    /// <summary>
    /// Nome visualizzato dell'impostazione
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Descrizione dell'impostazione
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gruppo di appartenenza dell'impostazione
    /// </summary>
    public string Group { get; set; } = string.Empty;
    
    /// <summary>
    /// Tipo di dato dell'impostazione (string, int, bool, datetime, json, ecc.)
    /// </summary>
    public string DataType { get; set; } = string.Empty;
    
    /// <summary>
    /// Indica se il valore deve essere mascherato nell'interfaccia (es. password)
    /// </summary>
    public bool IsMasked { get; set; }
    
    /// <summary>
    /// Indica se il valore deve essere cifrato nel database
    /// </summary>
    public bool IsEncrypted { get; set; }
    
    /// <summary>
    /// Indica se l'impostazione è obbligatoria
    /// </summary>
    public bool IsRequired { get; set; }
    
    /// <summary>
    /// Indica se l'impostazione è di sola lettura
    /// </summary>
    public bool IsReadOnly { get; set; }
    
    /// <summary>
    /// Valore predefinito dell'impostazione
    /// </summary>
    public string DefaultValue { get; set; } = string.Empty;
    
    /// <summary>
    /// Valore corrente dell'impostazione
    /// </summary>
    public string Value { get; set; } = string.Empty;
}