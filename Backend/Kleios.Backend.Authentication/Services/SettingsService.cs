using Kleios.Database.Context;
using Kleios.Database.Models;
using Kleios.Shared;
using Microsoft.EntityFrameworkCore;

namespace Kleios.Backend.Authentication.Services;

/// <summary>
/// Service for application settings management
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly KleiosDbContext _context;

    public SettingsService(KleiosDbContext context)
    {
        _context = context;
    }

    public async Task<Option<IEnumerable<AppSetting>>> GetAllSettingsAsync()
    {
        var settings = await _context.AppSettings.ToListAsync();
        return Option<IEnumerable<AppSetting>>.Success(settings);
    }

    public async Task<Option<IEnumerable<AppSetting>>> GetSettingsByCategoryAsync(string category)
    {
        var settings = await _context.AppSettings
            .Where(s => s.Category == category)
            .ToListAsync();
        
        return Option<IEnumerable<AppSetting>>.Success(settings);
    }

    public async Task<Option<AppSetting>> GetSettingByKeyAsync(string key)
    {
        var setting = await _context.AppSettings
            .FirstOrDefaultAsync(s => s.Key == key);
        
        if (setting == null)
        {
            return Option<AppSetting>.NotFound($"Impostazione con chiave '{key}' non trovata");
        }
        
        return Option<AppSetting>.Success(setting);
    }

    public async Task<Option<AppSetting>> UpdateSettingAsync(string key, string? value)
    {
        var setting = await _context.AppSettings
            .FirstOrDefaultAsync(s => s.Key == key);
        
        if (setting == null)
        {
            return Option<AppSetting>.NotFound($"Impostazione con chiave '{key}' non trovata");
        }
        
        if (setting.IsReadOnly)
        {
            return Option<AppSetting>.Forbidden("Questa impostazione è di sola lettura e non può essere modificata");
        }
        
        if (setting.IsRequired && string.IsNullOrEmpty(value))
        {
            return Option<AppSetting>.ValidationError("Questa impostazione è obbligatoria e non può essere vuota");
        }
        
        // Valida il valore in base al tipo di dato
        if (!ValidateValue(value, setting.DataType))
        {
            return Option<AppSetting>.ValidationError($"Il valore fornito non è valido per il tipo di dato {setting.DataType}");
        }
        
        setting.Value = value;
        setting.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return Option<AppSetting>.Success(setting);
    }

    public async Task<Option<AppSetting>> CreateSettingAsync(
        string key, 
        string? value, 
        string description, 
        string dataType, 
        bool isRequired, 
        bool isReadOnly, 
        string category)
    {
        var existingSetting = await _context.AppSettings
            .FirstOrDefaultAsync(s => s.Key == key);
        
        if (existingSetting != null)
        {
            return Option<AppSetting>.Conflict($"Un'impostazione con la chiave '{key}' esiste già");
        }
        
        if (isRequired && string.IsNullOrEmpty(value))
        {
            return Option<AppSetting>.ValidationError("Questa impostazione è obbligatoria e non può essere vuota");
        }
        
        // Valida il valore in base al tipo di dato
        if (!ValidateValue(value, dataType))
        {
            return Option<AppSetting>.ValidationError($"Il valore fornito non è valido per il tipo di dato {dataType}");
        }
        
        var setting = new AppSetting
        {
            Key = key,
            Value = value,
            Description = description,
            DataType = dataType,
            IsRequired = isRequired,
            IsReadOnly = isReadOnly,
            Category = category,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.AppSettings.Add(setting);
        await _context.SaveChangesAsync();
        
        return Option<AppSetting>.Success(setting);
    }

    public async Task<Option> DeleteSettingAsync(string key)
    {
        var setting = await _context.AppSettings
            .FirstOrDefaultAsync(s => s.Key == key);
        
        if (setting == null)
        {
            return Option.NotFound($"Impostazione con chiave '{key}' non trovata");
        }
        
        if (setting.IsReadOnly)
        {
            return Option.Forbidden("Questa impostazione è di sola lettura e non può essere eliminata");
        }
        
        _context.AppSettings.Remove(setting);
        await _context.SaveChangesAsync();
        
        return Option.Success();
    }
    
    private bool ValidateValue(string? value, string dataType)
    {
        if (string.IsNullOrEmpty(value))
        {
            return true; // Valori nulli sono validi a meno che non siano obbligatori (già controllato)
        }
        
        switch (dataType.ToLower())
        {
            case "string":
                return true; // Qualsiasi stringa è valida
            case "int":
            case "integer":
                return int.TryParse(value, out _);
            case "bool":
            case "boolean":
                return bool.TryParse(value, out _);
            case "decimal":
            case "number":
                return decimal.TryParse(value, out _);
            case "datetime":
            case "date":
                return DateTime.TryParse(value, out _);
            case "json":
                try
                {
                    System.Text.Json.JsonDocument.Parse(value);
                    return true;
                }
                catch
                {
                    return false;
                }
            default:
                return true; // Per tipi di dati personalizzati, consideriamo valido qualsiasi valore
        }
    }
}