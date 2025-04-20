namespace Kleios.Shared.Authorization;

/// <summary>
/// Attributo per definire i metadati di un permesso
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class PermissionAttribute : Attribute
{
    /// <summary>
    /// Nome visualizzato del permesso
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Descrizione del permesso
    /// </summary>
    public string Description { get; }
    
    /// <summary>
    /// Inizializza una nuova istanza dell'attributo Permesso
    /// </summary>
    /// <param name="name">Nome visualizzato del permesso</param>
    /// <param name="description">Descrizione del permesso</param>
    public PermissionAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }
}