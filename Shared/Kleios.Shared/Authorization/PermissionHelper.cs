using System.Reflection;

namespace Kleios.Shared.Authorization;

/// <summary>
/// Helper per lavorare con i permessi definiti attraverso attributi
/// </summary>
public static class PermissionHelper
{
    /// <summary>
    /// Ottiene tutti i permessi definiti nell'applicazione con i relativi metadati
    /// </summary>
    /// <returns>Elenco di permessi con nome e descrizione</returns>
    public static List<PermissionInfo> GetAllPermissions()
    {
        var permissions = new List<PermissionInfo>();
        
        // Ottieni tutti i tipi annidati nella classe AppPermissions
        var permissionCategories = typeof(AppPermissions).GetNestedTypes(BindingFlags.Public | BindingFlags.Static);
        
        foreach (var category in permissionCategories)
        {
            // Ottieni tutte le costanti pubbliche statiche
            var fields = category.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            
            foreach (var field in fields)
            {
                // Verifica che sia una costante di tipo string
                if (field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
                {
                    // Ottieni il valore del permesso (la stringa della costante)
                    var permissionValue = field.GetValue(null)?.ToString() ?? string.Empty;
                    
                    // Ottieni l'attributo PermissionAttribute se presente
                    var attribute = field.GetCustomAttribute<PermissionAttribute>();
                    
                    if (attribute != null)
                    {
                        permissions.Add(new PermissionInfo
                        {
                            Value = permissionValue,
                            Name = attribute.Name,
                            Description = attribute.Description,
                            Category = category.Name
                        });
                    }
                }
            }
        }
        
        return permissions;
    }
    
    /// <summary>
    /// Ottiene i metadati di un permesso specifico
    /// </summary>
    /// <param name="permissionValue">Il valore del permesso</param>
    /// <returns>Informazioni sul permesso, o null se non trovato</returns>
    public static PermissionInfo? GetPermissionInfo(string permissionValue)
    {
        return GetAllPermissions().FirstOrDefault(p => p.Value == permissionValue);
    }
}

/// <summary>
/// Classe che rappresenta le informazioni di un permesso
/// </summary>
public class PermissionInfo
{
    /// <summary>
    /// Valore del permesso usato nelle policy
    /// </summary>
    public string Value { get; set; } = string.Empty;
    
    /// <summary>
    /// Nome visualizzato del permesso
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Descrizione del permesso
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Categoria a cui appartiene il permesso
    /// </summary>
    public string Category { get; set; } = string.Empty;
}