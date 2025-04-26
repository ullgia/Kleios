using Kleios.Shared.Attributes;

namespace Kleios.Shared.Settings;

/// <summary>
/// Impostazioni generali dell'applicazione
/// </summary>
[SettingGroup("General", "Impostazioni generali dell'applicazione")]
public class GeneralSettingsModel
{
    /// <summary>
    /// Nome dell'applicazione
    /// </summary>
    [Setting("8F56A3F1-6D9B-4E8F-8321-A27C2B6CBED5", "General:ApplicationName", "Nome dell'applicazione", "General")]
    public string ApplicationName { get; set; } = "Kleios";
    
    /// <summary>
    /// Indica se la modalità di debug è attiva
    /// </summary>
    [Setting("2A3D12B7-CE5F-4B78-A620-8F782B93107C", "General:DebugMode", "Modalità di debug attiva", "General")]
    public bool DebugMode { get; set; } = false;
    
    /// <summary>
    /// Lingua predefinita dell'applicazione
    /// </summary>
    [Setting("6C9F4E31-D61A-4A9D-B23C-7A58DE23FD8A", "General:DefaultLanguage", "Lingua predefinita dell'applicazione", "General")]
    public string DefaultLanguage { get; set; } = "it-IT";
}