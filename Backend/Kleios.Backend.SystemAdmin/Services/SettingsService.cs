using Kleios.Backend.Shared;
using Kleios.Database.Context;
using Kleios.Database.Models;
using Kleios.Shared;
using Kleios.Shared.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kleios.Backend.SharedInfrastructure.Services;

/// <summary>
/// Implementazione del servizio di gestione delle impostazioni a livello di database
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly KleiosDbContext _dbContext;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(KleiosDbContext dbContext, ILogger<SettingsService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Option<IEnumerable<SettingMetadata>>> GetAllSettingsAsync()
    {
        var settings = await _dbContext.AppSettings.ToListAsync();
        return Option<IEnumerable<SettingMetadata>>.Success(settings.Select(MapToSettingMetadata));
    }

    /// <inheritdoc />
    public async Task<Option<IEnumerable<SettingMetadata>>> GetSettingsByCategoryAsync(string category)
    {
        var settings = await _dbContext.AppSettings
            .Where(s => s.Category == category)
            .ToListAsync();
        
        return Option<IEnumerable<SettingMetadata>>.Success(settings.Select(MapToSettingMetadata));
    }

    /// <inheritdoc />
    public async Task<Option<SettingMetadata>> GetSettingByKeyAsync(string key)
    {
        var setting = await _dbContext.AppSettings
            .FirstOrDefaultAsync(s => s.Key == key);
        
        if (setting == null)
        {
            return Option<SettingMetadata>.Failure($"Impostazione con chiave '{key}' non trovata");
        }
        
        return Option<SettingMetadata>.Success(MapToSettingMetadata(setting));
    }

    /// <inheritdoc />
    public async Task<Option<SettingMetadata>> UpdateSettingAsync(string key, string value)
    {
        var setting = await _dbContext.AppSettings
            .FirstOrDefaultAsync(s => s.Key == key);
        
        if (setting == null)
        {
            return Option<SettingMetadata>.Failure($"Impostazione con chiave '{key}' non trovata");
        }
        
        setting.Value = value;
        setting.UpdatedAt = DateTime.UtcNow;
        
        await _dbContext.SaveChangesAsync();
        
        return Option<SettingMetadata>.Success(MapToSettingMetadata(setting));
    }

    /// <inheritdoc />
    public async Task<Option<SettingMetadata>> CreateSettingAsync(SettingDto dto)
    {
        var existingSetting = await _dbContext.AppSettings
            .FirstOrDefaultAsync(s => s.Key == dto.Key);
        
        if (existingSetting != null)
        {
            return Option<SettingMetadata>.Failure($"Impostazione con chiave '{dto.Key}' già esistente");
        }
        
        var newSetting = new AppSetting
        {
            Key = dto.Key,
            Value = dto.Value,
            Description = dto.Description,
            DataType = dto.DataType,
            IsRequired = dto.IsRequired,
            IsReadOnly = dto.IsReadOnly,
            Category = dto.Category,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _dbContext.AppSettings.Add(newSetting);
        await _dbContext.SaveChangesAsync();
        
        return Option<SettingMetadata>.Success(MapToSettingMetadata(newSetting));
    }

    /// <inheritdoc />
    public async Task<Option<SettingMetadata>> CreateSettingAsync(string key, string value, string description, string dataType, bool isRequired, bool isReadOnly, string category)
    {
        var dto = new SettingDto
        {
            Key = key,
            Value = value,
            Description = description,
            DataType = dataType,
            IsRequired = isRequired,
            IsReadOnly = isReadOnly,
            Category = category
        };
        
        return await CreateSettingAsync(dto);
    }

    /// <inheritdoc />
    public async Task<Option> DeleteSettingAsync(string key)
    {
        var setting = await _dbContext.AppSettings
            .FirstOrDefaultAsync(s => s.Key == key);
        
        if (setting == null)
        {
            return Option.Failure($"Impostazione con chiave '{key}' non trovata");
        }
        
        _dbContext.AppSettings.Remove(setting);
        await _dbContext.SaveChangesAsync();
        
        return Option.Success();
    }
    
    // Metodo helper per mappare AppSetting a SettingMetadata
    private static SettingMetadata MapToSettingMetadata(AppSetting setting)
    {
        return new SettingMetadata
        {
            Key = setting.Key,
            Value = setting.Value,
            Description = setting.Description,
            DataType = setting.DataType,
            IsRequired = setting.IsRequired,
            IsReadOnly = setting.IsReadOnly,
            Category = setting.Category
        };
    }
}