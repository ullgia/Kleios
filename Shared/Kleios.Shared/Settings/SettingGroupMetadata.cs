namespace Kleios.Shared.Settings;

/// <summary>
/// Rappresenta i metadati di un gruppo di impostazioni
/// </summary>
public class SettingGroupMetadata
{
    /// <summary>
    /// Nome del gruppo
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Descrizione del gruppo
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Ordine di visualizzazione del gruppo
    /// </summary>
    public int Order { get; set; }
    
    /// <summary>
    /// Elenco delle impostazioni nel gruppo
    /// </summary>
    public List<SettingMetadata> Settings { get; set; } = new();
}