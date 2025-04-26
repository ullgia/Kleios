using System;

namespace Kleios.Shared.Attributes;

/// <summary>
/// Attributo che definisce un'impostazione dell'applicazione
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class SettingAttribute : Attribute
{
    /// <summary>
    /// Identificatore univoco dell'impostazione
    /// </summary>
    public Guid Id { get; }
    
    /// <summary>
    /// Nome dell'impostazione (es. "Jwt:Secret")
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Descrizione dell'impostazione
    /// </summary>
    public string Description { get; set; } = "";
    
    /// <summary>
    /// Gruppo di appartenenza dell'impostazione
    /// </summary>
    public string Group { get; set; } = "General";
    
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
    /// Tipo di dato dell'impostazione (string, int, bool, datetime, json, ecc.)
    /// </summary>
    public string DataType { get; set; } = "string";

    /// <summary>
    /// Crea una nuova istanza dell'attributo Setting con un ID specifico
    /// </summary>
    /// <param name="id">ID univoco dell'impostazione (dovrebbe essere un GUID statico)</param>
    /// <param name="name">Nome dell'impostazione (es. "Jwt:Secret")</param>
    /// <param name="description">Descrizione dell'impostazione</param>
    /// <param name="group">Gruppo di appartenenza</param>
    public SettingAttribute(string id, string name, string description = "", string group = "General")
    {
        Id = Guid.Parse(id);
        Name = name;
        Description = description;
        Group = group;
    }
}