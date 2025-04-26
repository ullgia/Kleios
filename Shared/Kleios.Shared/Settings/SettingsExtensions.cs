using System.Reflection;
using Kleios.Shared.Attributes;

namespace Kleios.Shared.Settings;

/// <summary>
/// Estensioni per estrarre i metadati dalle impostazioni
/// </summary>
public static class SettingsExtensions
{
    /// <summary>
    /// Estrae i metadati da un modello di impostazioni
    /// </summary>
    public static IEnumerable<SettingMetadata> ExtractMetadata(this AppSettingsModel model)
    {
        var result = new List<SettingMetadata>();
        ExtractMetadataRecursive(model, "", result);
        return result;
    }
    
    /// <summary>
    /// Estrae i metadati di un gruppo specifico da un modello di impostazioni
    /// </summary>
    public static IEnumerable<SettingMetadata> ExtractMetadataByGroup(this AppSettingsModel model, string group)
    {
        return ExtractMetadata(model).Where(m => m.Group == group);
    }
    
    /// <summary>
    /// Estrae tutti i gruppi di impostazioni con i relativi metadati
    /// </summary>
    public static IEnumerable<SettingGroupMetadata> ExtractGroups(this AppSettingsModel model)
    {
        var allMetadata = ExtractMetadata(model);
        var groups = allMetadata.Select(m => m.Group).Distinct();
        
        var result = new List<SettingGroupMetadata>();
        
        foreach (var group in groups)
        {
            var groupMetadata = new SettingGroupMetadata
            {
                Name = group,
                Settings = allMetadata.Where(m => m.Group == group).ToList()
            };
            
            // Trova la descrizione del gruppo dal primo elemento con attributo SettingGroup
            var firstSetting = allMetadata.FirstOrDefault(m => m.Group == group);
            if (firstSetting != null)
            {
                var settingPath = firstSetting.Key.Split(':')[0];
                var modelType = GetModelTypeByName(model, settingPath);
                
                if (modelType != null)
                {
                    var groupAttribute = modelType.GetCustomAttribute<SettingGroupAttribute>();
                    if (groupAttribute != null)
                    {
                        groupMetadata.Description = groupAttribute.Description;
                        groupMetadata.Order = groupAttribute.Order;
                    }
                }
            }
            
            result.Add(groupMetadata);
        }
        
        return result.OrderBy(g => g.Order);
    }
    
    private static void ExtractMetadataRecursive(object model, string path, List<SettingMetadata> result)
    {
        if (model == null) return;
        
        var modelType = model.GetType();
        var properties = modelType.GetProperties();
        
        // Ottieni gli attributi del gruppo, se presenti
        var groupAttribute = modelType.GetCustomAttribute<SettingGroupAttribute>();
        var groupName = groupAttribute?.Name ?? "General";
        
        foreach (var property in properties)
        {
            var settingAttribute = property.GetCustomAttribute<SettingAttribute>();
            if (settingAttribute != null)
            {
                var propertyType = property.PropertyType;
                var propertyValue = property.GetValue(model);
                
                // Se è un tipo primitivo o stringa, aggiungi i metadati
                if (IsSimpleType(propertyType))
                {
                    result.Add(new SettingMetadata
                    {
                        Id = settingAttribute.Id,
                        Key = settingAttribute.Name,
                        DisplayName = string.IsNullOrEmpty(settingAttribute.Description) ? property.Name : settingAttribute.Description,
                        Description = settingAttribute.Description,
                        Group = settingAttribute.Group ?? groupName,
                        DataType = settingAttribute.DataType,
                        IsEncrypted = settingAttribute.IsEncrypted,
                        IsMasked = settingAttribute.IsMasked,
                        IsRequired = settingAttribute.IsRequired,
                        IsReadOnly = settingAttribute.IsReadOnly,
                        DefaultValue = ConvertToString(propertyValue),
                        Value = ConvertToString(propertyValue)
                    });
                }
                // Se è un tipo complesso, esplora ricorsivamente
                else if (propertyValue != null)
                {
                    ExtractMetadataRecursive(propertyValue, 
                        string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}", 
                        result);
                }
            }
        }
    }
    
    private static Type? GetModelTypeByName(AppSettingsModel model, string sectionName)
    {
        var property = typeof(AppSettingsModel).GetProperty(sectionName);
        return property?.PropertyType;
    }
    
    private static bool IsSimpleType(Type? type)
    {
        if (type == null) return false;
        
        return type.IsPrimitive 
               || type == typeof(string) 
               || type == typeof(decimal) 
               || type == typeof(DateTime) 
               || type == typeof(TimeSpan) 
               || type == typeof(Guid) 
               || type.IsEnum 
               || (Nullable.GetUnderlyingType(type) != null && IsSimpleType(Nullable.GetUnderlyingType(type)));
    }
    
    private static string ConvertToString(object? value)
    {
        if (value == null) return string.Empty;
        
        if (value is string strValue) return strValue;
        if (value is DateTime dateTime) return dateTime.ToString("O");
        
        return value.ToString()!;
    }
}