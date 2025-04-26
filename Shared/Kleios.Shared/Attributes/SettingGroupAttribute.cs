using System;

namespace Kleios.Shared.Attributes;

/// <summary>
/// Attributo che definisce un gruppo di impostazioni
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SettingGroupAttribute : Attribute
{
    /// <summary>
    /// Nome del gruppo di impostazioni
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Descrizione del gruppo di impostazioni
    /// </summary>
    public string Description { get; }
    
    /// <summary>
    /// Ordine di visualizzazione del gruppo (opzionale)
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Crea una nuova istanza dell'attributo SettingGroup
    /// </summary>
    /// <param name="name">Nome del gruppo</param>
    /// <param name="description">Descrizione del gruppo</param>
    public SettingGroupAttribute(string name, string description = "")
    {
        Name = name;
        Description = description;
    }
}