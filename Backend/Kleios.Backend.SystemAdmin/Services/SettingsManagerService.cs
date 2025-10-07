using System.Reflection;
using System.Text.Json;
using Kleios.Backend.Shared;
using Kleios.Database.Models;
using Kleios.Shared.Attributes;
using Kleios.Shared.Settings;
using Microsoft.Extensions.Logging;

namespace Kleios.Backend.SystemAdmin.Services;

/// <summary>
/// Servizio per la gestione delle impostazioni dell'applicazione
/// </summary>
public class SettingsManagerService : ISettingsManagerService
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SettingsManagerService> _logger;
    private AppSettingsModel _appSettings = new();
    private bool _initialized = false;
    private readonly Dictionary<string, PropertyInfo> _propertyMap = new();

    public SettingsManagerService(
        ISettingsService settingsService,
        ILogger<SettingsManagerService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <summary>
    /// Inizializza il servizio caricando tutte le impostazioni dal database
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        try
        {
            _appSettings = new AppSettingsModel();
            
            // Mappa tutte le proprietà disponibili
            BuildPropertyMap(_appSettings);
            
            // Carica le impostazioni esistenti dal database
            var settingsOption = await _settingsService.GetAllSettingsAsync();
            if (settingsOption.IsSuccess)
            {
                // Per ogni impostazione trovata nel database, aggiorna il modello
                foreach (var setting in settingsOption.Value)
                {
                    // Cerca la proprietà corrispondente in base al Name dell'attributo Setting
                    var propertyEntry = _propertyMap.FirstOrDefault(p => 
                    {
                        var attr = p.Value.GetCustomAttribute<SettingAttribute>();
                        return attr != null && attr.Name == setting.Key;
                    });

                    if (!propertyEntry.Equals(default(KeyValuePair<string, PropertyInfo>)))
                    {
                        try
                        {
                            // Ottieni l'oggetto contenitore e la proprietà
                            var (container, property) = GetPropertyContainer(propertyEntry.Key);
                            if (container != null && property != null)
                            {
                                // Converti il valore dal database al tipo della proprietà
                                var convertedValue = ConvertToType(setting.Value, property.PropertyType);
                                property.SetValue(container, convertedValue);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Errore durante l'impostazione del valore per {Key}", setting.Key);
                        }
                    }
                }

                _initialized = true;
                _logger.LogInformation("Configurazione caricata con successo dal database");
            }
            else
            {
                _logger.LogWarning("Impossibile caricare le impostazioni dal database. Utilizzo dei valori predefiniti.");
                // Inizializza con valori di default e salva nel database
                await SaveSettingsAsync(_appSettings);
                _initialized = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il caricamento delle impostazioni dal database");
        }
    }

    #region Metodi di ISettingsManager

    /// <summary>
    /// Ottiene tutte le impostazioni come modello strutturato
    /// </summary>
    public AppSettingsModel GetSettings()
    {
        if (!_initialized)
        {
            _logger.LogWarning("Il servizio di configurazione non è stato inizializzato");
            InitializeAsync().GetAwaiter().GetResult();
        }

        return _appSettings;
    }

    /// <summary>
    /// Ottiene una sezione specifica di impostazioni
    /// </summary>
    public T GetSection<T>(string sectionName) where T : class, new()
    {
        if (!_initialized)
        {
            _logger.LogWarning("Il servizio di configurazione non è stato inizializzato");
            InitializeAsync().GetAwaiter().GetResult();
        }

        // Cerca la proprietà corrispondente nella classe AppSettingsModel
        var property = typeof(AppSettingsModel).GetProperty(sectionName);
        if (property != null && property.PropertyType == typeof(T))
        {
            return (T)(property.GetValue(_appSettings) ?? new T());
        }

        return new T();
    }

    /// <summary>
    /// Ottiene un valore di impostazione specifico in base al nome (es. "Jwt:Secret")
    /// </summary>
    public string GetValue(string settingName, string defaultValue = "")
    {
        if (!_initialized)
        {
            _logger.LogWarning("Il servizio di configurazione non è stato inizializzato");
            InitializeAsync().GetAwaiter().GetResult();
        }

        var propertyEntry = _propertyMap.FirstOrDefault(p => 
        {
            var attr = p.Value.GetCustomAttribute<SettingAttribute>();
            return attr != null && attr.Name == settingName;
        });

        if (!propertyEntry.Equals(default(KeyValuePair<string, PropertyInfo>)))
        {
            try
            {
                var (container, property) = GetPropertyContainer(propertyEntry.Key);
                if (container != null && property != null)
                {
                    var value = property.GetValue(container);
                    return value?.ToString() ?? defaultValue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero del valore per {Key}", settingName);
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// Ottiene un valore di impostazione come intero
    /// </summary>
    public int GetIntValue(string settingName, int defaultValue = 0)
    {
        var stringValue = GetValue(settingName, defaultValue.ToString());
        return int.TryParse(stringValue, out var intValue) ? intValue : defaultValue;
    }

    /// <summary>
    /// Ottiene un valore di impostazione come booleano
    /// </summary>
    public bool GetBoolValue(string settingName, bool defaultValue = false)
    {
        var stringValue = GetValue(settingName, defaultValue.ToString());
        return bool.TryParse(stringValue, out var boolValue) ? boolValue : defaultValue;
    }

    /// <summary>
    /// Ottiene un valore di impostazione come double
    /// </summary>
    public double GetDoubleValue(string settingName, double defaultValue = 0)
    {
        var stringValue = GetValue(settingName, defaultValue.ToString());
        return double.TryParse(stringValue, out var doubleValue) ? doubleValue : defaultValue;
    }

    /// <summary>
    /// Ottiene un valore di impostazione come DateTime
    /// </summary>
    public DateTime GetDateTimeValue(string settingName, DateTime? defaultValue = null)
    {
        defaultValue ??= DateTime.MinValue;
        var stringValue = GetValue(settingName, defaultValue.Value.ToString("O"));
        return DateTime.TryParse(stringValue, out var dateTimeValue) ? dateTimeValue : defaultValue.Value;
    }

    /// <summary>
    /// Ottiene i metadati di tutte le impostazioni
    /// </summary>
    public IEnumerable<SettingMetadata> GetAllSettingMetadata()
    {
        return _appSettings.ExtractMetadata();
    }

    /// <summary>
    /// Ottiene i metadati di un gruppo specifico di impostazioni
    /// </summary>
    public IEnumerable<SettingMetadata> GetSettingMetadataByGroup(string group)
    {
        return _appSettings.ExtractMetadataByGroup(group);
    }

    /// <summary>
    /// Ottiene tutti i gruppi di impostazioni
    /// </summary>
    public IEnumerable<SettingGroupMetadata> GetAllGroups()
    {
        return _appSettings.ExtractGroups();
    }

    #endregion

    #region Metodi di ISettingsManagerService

    /// <summary>
    /// Salva l'intero modello delle impostazioni nel database
    /// </summary>
    public async Task<bool> SaveSettingsAsync(AppSettingsModel settings)
    {
        try
        {
            // Aggiorna il modello locale
            _appSettings = settings;
            BuildPropertyMap(_appSettings);

            // Ottiene tutte le proprietà con attributo Setting
            var properties = GetAllSettingProperties();
            
            foreach (var (path, property) in properties)
            {
                var attribute = property.GetCustomAttribute<SettingAttribute>();
                if (attribute == null) continue;

                var (container, prop) = GetPropertyContainer(path);
                if (container == null || prop == null) continue;

                var value = prop.GetValue(container);
                var stringValue = ConvertToString(value ?? string.Empty);

                // Verifica se l'impostazione esiste già
                var existingOption = await _settingsService.GetSettingByKeyAsync(attribute.Name);
                
                if (existingOption.IsSuccess)
                {
                    // Aggiorna l'impostazione esistente
                    await _settingsService.UpdateSettingAsync(attribute.Name, stringValue);
                }
                else
                {
                    // Crea una nuova impostazione
                    string dataType = GetDataType(prop.PropertyType);
                    await _settingsService.CreateSettingAsync(
                        attribute.Name,
                        stringValue,
                        attribute.Description,
                        dataType,
                        attribute.IsRequired,
                        attribute.IsReadOnly,
                        attribute.Group);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il salvataggio delle impostazioni");
            return false;
        }
    }

    /// <summary>
    /// Salva una sezione specifica di impostazioni
    /// </summary>
    public async Task<bool> SaveSectionAsync<T>(string sectionName, T section) where T : class
    {
        try
        {
            // Aggiorna la sezione nel modello locale
            var sectionProperty = typeof(AppSettingsModel).GetProperty(sectionName);
            if (sectionProperty != null && sectionProperty.PropertyType == typeof(T))
            {
                sectionProperty.SetValue(_appSettings, section);
            }
            else
            {
                _logger.LogWarning("Sezione {SectionName} non trovata nel modello delle impostazioni", sectionName);
                return false;
            }

            // Ricrea la mappa delle proprietà
            BuildPropertyMap(_appSettings);

            // Ottiene tutte le proprietà della sezione con attributo Setting
            var properties = GetSectionSettingProperties(sectionName);
            
            foreach (var (path, property) in properties)
            {
                var attribute = property.GetCustomAttribute<SettingAttribute>();
                if (attribute == null) continue;

                var (container, prop) = GetPropertyContainer(path);
                if (container == null || prop == null) continue;

                var value = prop.GetValue(container);
                var stringValue = ConvertToString(value ?? string.Empty);

                // Verifica se l'impostazione esiste già
                var existingOption = await _settingsService.GetSettingByKeyAsync(attribute.Name);
                
                if (existingOption.IsSuccess)
                {
                    // Aggiorna l'impostazione esistente
                    await _settingsService.UpdateSettingAsync(attribute.Name, stringValue);
                }
                else
                {
                    // Crea una nuova impostazione
                    string dataType = GetDataType(prop.PropertyType);
                    await _settingsService.CreateSettingAsync(
                        attribute.Name,
                        stringValue,
                        attribute.Description,
                        dataType,
                        attribute.IsRequired,
                        attribute.IsReadOnly,
                        attribute.Group);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il salvataggio della sezione {SectionName}", sectionName);
            return false;
        }
    }

    /// <summary>
    /// Aggiorna un'impostazione specifica
    /// </summary>
    public async Task<bool> UpdateSettingAsync(string settingName, string value)
    {
        try
        {
            // Cerca la proprietà corrispondente in base al Name dell'attributo Setting
            var propertyEntry = _propertyMap.FirstOrDefault(p => 
            {
                var attr = p.Value.GetCustomAttribute<SettingAttribute>();
                return attr != null && attr.Name == settingName;
            });

            if (!propertyEntry.Equals(default(KeyValuePair<string, PropertyInfo>)))
            {
                // Ottieni l'oggetto contenitore e la proprietà
                var (container, property) = GetPropertyContainer(propertyEntry.Key);
                if (container != null && property != null)
                {
                    // Converti il valore al tipo della proprietà
                    var convertedValue = ConvertToType(value, property.PropertyType);
                    property.SetValue(container, convertedValue);

                    // Aggiorna il valore nel database
                    var updateOption = await _settingsService.UpdateSettingAsync(settingName, value);
                    if (!updateOption.IsSuccess)
                    {
                        _logger.LogWarning("Impossibile aggiornare l'impostazione {Key}", settingName);
                        return false;
                    }

                    return true;
                }
            }

            _logger.LogWarning("Impostazione {Key} non trovata", settingName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'aggiornamento dell'impostazione {Key}", settingName);
            return false;
        }
    }

    #endregion

    #region Metodi di utilità

    /// <summary>
    /// Costruisce la mappa delle proprietà con attributo Setting
    /// </summary>
    private void BuildPropertyMap(object model, string path = "")
    {
        var modelType = model.GetType();
        var properties = modelType.GetProperties();

        foreach (var property in properties)
        {
            var propertyPath = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";
            var attribute = property.GetCustomAttribute<SettingAttribute>();

            if (attribute != null)
            {
                // Memorizza il percorso della proprietà
                _propertyMap[propertyPath] = property;
            }

            // Se la proprietà è un tipo complesso, esplora ricorsivamente
            var value = property.GetValue(model);
            if (value != null && !IsSimpleType(property.PropertyType))
            {
                BuildPropertyMap(value, propertyPath);
            }
        }
    }

    /// <summary>
    /// Ottiene tutte le proprietà con attributo Setting
    /// </summary>
    private List<(string Path, PropertyInfo Property)> GetAllSettingProperties(object? model = null, string path = "")
    {
        var result = new List<(string, PropertyInfo)>();
        model ??= _appSettings;
        var modelType = model.GetType();
        var properties = modelType.GetProperties();

        foreach (var property in properties)
        {
            var propertyPath = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";
            var attribute = property.GetCustomAttribute<SettingAttribute>();

            if (attribute != null)
            {
                result.Add((propertyPath, property));
            }

            // Se la proprietà è un tipo complesso, esplora ricorsivamente
            var value = property.GetValue(model);
            if (value != null && !IsSimpleType(property.PropertyType))
            {
                result.AddRange(GetAllSettingProperties(value, propertyPath));
            }
        }

        return result;
    }

    /// <summary>
    /// Ottiene le proprietà di una sezione specifica
    /// </summary>
    private List<(string Path, PropertyInfo Property)> GetSectionSettingProperties(string sectionName)
    {
        var result = new List<(string, PropertyInfo)>();
        var sectionProperty = typeof(AppSettingsModel).GetProperty(sectionName);
        
        if (sectionProperty == null)
        {
            return result;
        }

        var sectionValue = sectionProperty.GetValue(_appSettings);
        if (sectionValue == null)
        {
            return result;
        }

        return GetAllSettingProperties(sectionValue, sectionName);
    }

    /// <summary>
    /// Ottiene il contenitore e la proprietà dato il percorso
    /// </summary>
    private (object? Container, PropertyInfo? Property) GetPropertyContainer(string path)
    {
        var parts = path.Split('.');
        object? container = _appSettings;
        PropertyInfo? property = null;

        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            var containerType = container.GetType();
            property = containerType.GetProperty(part);

            if (property == null)
            {
                return (null, null);
            }

            if (i < parts.Length - 1)
            {
                container = property.GetValue(container);
                if (container == null)
                {
                    return (null, null);
                }
            }
        }

        return (container, property);
    }

    /// <summary>
    /// Converte un valore in stringa
    /// </summary>
    private string ConvertToString(object value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (value is string stringValue)
        {
            return stringValue;
        }

        if (value is DateTime dateTime)
        {
            return dateTime.ToString("O");
        }

        if (IsSimpleType(value.GetType()))
        {
            return value.ToString() ?? string.Empty;
        }

        // Per tipi complessi, serializza in JSON
        return JsonSerializer.Serialize(value);
    }

    /// <summary>
    /// Converte un valore stringa al tipo specificato
    /// </summary>
    private object? ConvertToType(string value, Type targetType)
    {
        if (string.IsNullOrEmpty(value))
        {
            return GetDefaultValue(targetType);
        }

        if (targetType == typeof(string))
        {
            return value;
        }
        
        if (targetType == typeof(int) || targetType == typeof(int?))
        {
            return int.TryParse(value, out var intValue) ? intValue : GetDefaultValue(targetType);
        }
        
        if (targetType == typeof(bool) || targetType == typeof(bool?))
        {
            return bool.TryParse(value, out var boolValue) ? boolValue : GetDefaultValue(targetType);
        }
        
        if (targetType == typeof(double) || targetType == typeof(double?))
        {
            return double.TryParse(value, out var doubleValue) ? doubleValue : GetDefaultValue(targetType);
        }
        
        if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
        {
            return DateTime.TryParse(value, out var dateTimeValue) ? dateTimeValue : GetDefaultValue(targetType);
        }
        
        if (targetType.IsEnum)
        {
            try
            {
                return Enum.Parse(targetType, value);
            }
            catch
            {
                return GetDefaultValue(targetType);
            }
        }
        
        // Per tipi complessi, deserializza da JSON
        try
        {
            return JsonSerializer.Deserialize(value, targetType) ?? GetDefaultValue(targetType);
        }
        catch
        {
            return GetDefaultValue(targetType);
        }
    }

    /// <summary>
    /// Ottiene il valore di default per un tipo
    /// </summary>
    private object? GetDefaultValue(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }

        return null;
    }

    /// <summary>
    /// Verifica se un tipo è semplice (primitivo, stringa, DateTime, ecc.)
    /// </summary>
    private bool IsSimpleType(Type type)
    {
        return type.IsPrimitive
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(TimeSpan)
            || type == typeof(Guid)
            || type.IsEnum
            || (Nullable.GetUnderlyingType(type) is Type underlyingType && IsSimpleType(underlyingType));
    }

    /// <summary>
    /// Ottiene il tipo di dato da un Type
    /// </summary>
    private string GetDataType(Type type)
    {
        if (type == typeof(int) || type == typeof(int?))
            return "int";
        if (type == typeof(long) || type == typeof(long?))
            return "long";
        if (type == typeof(bool) || type == typeof(bool?))
            return "bool";
        if (type == typeof(double) || type == typeof(double?) || type == typeof(decimal) || type == typeof(decimal?))
            return "decimal";
        if (type == typeof(DateTime) || type == typeof(DateTime?))
            return "datetime";
        if (type.IsEnum)
            return "enum";
        if (!IsSimpleType(type))
            return "json";
            
        return "string";
    }

    #endregion
}