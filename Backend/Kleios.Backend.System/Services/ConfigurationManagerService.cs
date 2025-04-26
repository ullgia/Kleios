using Kleios.Backend.Shared;
using Kleios.Database.Models;
using Microsoft.Extensions.Logging;

namespace Kleios.Backend.SystemAdmin.Services;

public interface IConfigurationManagerService
{
    Task InitializeAsync();
    string GetValue(string key, string defaultValue = "");
    int GetIntValue(string key, int defaultValue = 0);
    bool GetBoolValue(string key, bool defaultValue = false);
    double GetDoubleValue(string key, double defaultValue = 0);
    DateTime GetDateTimeValue(string key, DateTime? defaultValue = null);
    T GetObject<T>(string key, T defaultValue) where T : class, new();
    JwtConfig GetJwtConfig();
}

/// <summary>
/// Servizio che si occupa di caricare e gestire le impostazioni dell'applicazione dal database
/// </summary>
public class ConfigurationManagerService : IConfigurationManagerService
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<ConfigurationManagerService> _logger;
    private readonly Dictionary<string, string> _settings = new();
    private JwtConfig _jwtConfig = new();
    private bool _initialized = false;

    public ConfigurationManagerService(
        ISettingsService settingsService,
        ILogger<ConfigurationManagerService> logger)
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
            var result = await _settingsService.GetAllSettingsAsync();
            if (result.IsSuccess && result.Value != null)
            {
                // Memorizza tutte le impostazioni in un dizionario per un accesso rapido
                foreach (var setting in result.Value)
                {
                    _settings[setting.Key] = setting.Value ?? string.Empty;
                }

                // Inizializza la configurazione JWT
                await InitializeJwtConfigAsync();

                _initialized = true;
                _logger.LogInformation("Configurazione caricata con successo dal database: {SettingsCount} impostazioni trovate", _settings.Count);
            }
            else
            {
                _logger.LogWarning("Impossibile caricare le impostazioni dal database: {Message}", result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il caricamento delle impostazioni dal database");
        }
    }

    /// <summary>
    /// Ottiene un valore di impostazione come stringa
    /// </summary>
    public string GetValue(string key, string defaultValue = "")
    {
        if (!_initialized)
        {
            _logger.LogWarning("Il servizio di configurazione non è stato inizializzato");
            return defaultValue;
        }

        return _settings.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// Ottiene un valore di impostazione come intero
    /// </summary>
    public int GetIntValue(string key, int defaultValue = 0)
    {
        if (!_initialized)
        {
            _logger.LogWarning("Il servizio di configurazione non è stato inizializzato");
            return defaultValue;
        }

        if (_settings.TryGetValue(key, out var value) && int.TryParse(value, out var intValue))
        {
            return intValue;
        }

        return defaultValue;
    }

    /// <summary>
    /// Ottiene un valore di impostazione come booleano
    /// </summary>
    public bool GetBoolValue(string key, bool defaultValue = false)
    {
        if (!_initialized)
        {
            _logger.LogWarning("Il servizio di configurazione non è stato inizializzato");
            return defaultValue;
        }

        if (_settings.TryGetValue(key, out var value) && bool.TryParse(value, out var boolValue))
        {
            return boolValue;
        }

        return defaultValue;
    }

    /// <summary>
    /// Ottiene un valore di impostazione come double
    /// </summary>
    public double GetDoubleValue(string key, double defaultValue = 0)
    {
        if (!_initialized)
        {
            _logger.LogWarning("Il servizio di configurazione non è stato inizializzato");
            return defaultValue;
        }

        if (_settings.TryGetValue(key, out var value) && double.TryParse(value, out var doubleValue))
        {
            return doubleValue;
        }

        return defaultValue;
    }

    /// <summary>
    /// Ottiene un valore di impostazione come DateTime
    /// </summary>
    public DateTime GetDateTimeValue(string key, DateTime? defaultValue = null)
    {
        if (!_initialized)
        {
            _logger.LogWarning("Il servizio di configurazione non è stato inizializzato");
            return defaultValue ?? DateTime.UtcNow;
        }

        if (_settings.TryGetValue(key, out var value) && DateTime.TryParse(value, out var dateTimeValue))
        {
            return dateTimeValue;
        }

        return defaultValue ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Ottiene un valore di impostazione come oggetto JSON deserializzato
    /// </summary>
    public T GetObject<T>(string key, T defaultValue) where T : class, new()
    {
        if (!_initialized)
        {
            _logger.LogWarning("Il servizio di configurazione non è stato inizializzato");
            return defaultValue;
        }

        if (_settings.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(value) ?? defaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la deserializzazione dell'oggetto per la chiave {Key}", key);
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// Ottiene la configurazione JWT
    /// </summary>
    public JwtConfig GetJwtConfig()
    {
        if (!_initialized)
        {
            _logger.LogWarning("Il servizio di configurazione non è stato inizializzato");
            return new JwtConfig();
        }

        return _jwtConfig;
    }

    /// <summary>
    /// Inizializza la configurazione JWT dalle impostazioni del database
    /// </summary>
    private async Task InitializeJwtConfigAsync()
    {
        var jwtCategory = "JWT";
        var result = await _settingsService.GetSettingsByCategoryAsync(jwtCategory);
        
        if (!result.IsSuccess || result.Value == null)
        {
            _logger.LogWarning("Impossibile trovare le impostazioni JWT nel database, verranno utilizzate le impostazioni di default");
            await CreateDefaultJwtSettingsAsync();
            return;
        }

        var jwtSettings = result.Value.ToDictionary(s => s.Key, s => s.Value);
        
        _jwtConfig.SecretKey = GetSettingValue(jwtSettings, "JWT:SecretKey", _jwtConfig.SecretKey);
        _jwtConfig.Issuer = GetSettingValue(jwtSettings, "JWT:Issuer", _jwtConfig.Issuer);
        _jwtConfig.Audience = GetSettingValue(jwtSettings, "JWT:Audience", _jwtConfig.Audience);
        _jwtConfig.TokenValidityInMinutes = int.TryParse(GetSettingValue(jwtSettings, "JWT:TokenValidityInMinutes", _jwtConfig.TokenValidityInMinutes.ToString()), out var tokenValidity) 
            ? tokenValidity 
            : _jwtConfig.TokenValidityInMinutes;
        _jwtConfig.RefreshTokenValidityInDays = int.TryParse(GetSettingValue(jwtSettings, "JWT:RefreshTokenValidityInDays", _jwtConfig.RefreshTokenValidityInDays.ToString()), out var refreshTokenValidity) 
            ? refreshTokenValidity 
            : _jwtConfig.RefreshTokenValidityInDays;
    }

    /// <summary>
    /// Ottiene un valore di impostazione da un dizionario di impostazioni o restituisce il valore di default
    /// </summary>
    private string GetSettingValue(Dictionary<string, string?> settings, string key, string defaultValue)
    {
        return settings.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value) ? value : defaultValue;
    }

    /// <summary>
    /// Crea le impostazioni JWT di default nel database
    /// </summary>
    private async Task CreateDefaultJwtSettingsAsync()
    {
        try
        {
            var jwtCategory = "JWT";
            
            await CreateOrUpdateSettingAsync("JWT:SecretKey", _jwtConfig.SecretKey, 
                "Chiave segreta per la firma dei token JWT", "string", true, false, jwtCategory);
                
            await CreateOrUpdateSettingAsync("JWT:Issuer", _jwtConfig.Issuer, 
                "Emittente del token (chi ha generato il token)", "string", true, false, jwtCategory);
                
            await CreateOrUpdateSettingAsync("JWT:Audience", _jwtConfig.Audience, 
                "Pubblico destinatario del token", "string", true, false, jwtCategory);
                
            await CreateOrUpdateSettingAsync("JWT:TokenValidityInMinutes", _jwtConfig.TokenValidityInMinutes.ToString(), 
                "Durata di validità del token in minuti", "int", true, false, jwtCategory);
                
            await CreateOrUpdateSettingAsync("JWT:RefreshTokenValidityInDays", _jwtConfig.RefreshTokenValidityInDays.ToString(), 
                "Durata di validità del refresh token in giorni", "int", true, false, jwtCategory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la creazione delle impostazioni JWT di default");
        }
    }

    /// <summary>
    /// Crea o aggiorna un'impostazione nel database
    /// </summary>
    private async Task CreateOrUpdateSettingAsync(
        string key, 
        string value, 
        string description, 
        string dataType, 
        bool isRequired, 
        bool isReadOnly, 
        string category)
    {
        var getResult = await _settingsService.GetSettingByKeyAsync(key);
        
        if (getResult.IsSuccess)
        {
            // L'impostazione esiste già, aggiorniamola
            await _settingsService.UpdateSettingAsync(key, value);
        }
        else
        {
            // L'impostazione non esiste, creiamola
            await _settingsService.CreateSettingAsync(key, value, description, dataType, isRequired, isReadOnly, category);
        }
    }
}