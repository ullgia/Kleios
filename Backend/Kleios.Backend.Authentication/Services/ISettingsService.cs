using Kleios.Database.Models;
using Kleios.Shared;

namespace Kleios.Backend.Authentication.Services;

public interface ISettingsService
{
    Task<Option<IEnumerable<AppSetting>>> GetAllSettingsAsync();
    Task<Option<IEnumerable<AppSetting>>> GetSettingsByCategoryAsync(string category);
    Task<Option<AppSetting>> GetSettingByKeyAsync(string key);
    Task<Option<AppSetting>> UpdateSettingAsync(string key, string? value);
    Task<Option<AppSetting>> CreateSettingAsync(string key, string? value, string description, string dataType, bool isRequired, bool isReadOnly, string category);
    Task<Option> DeleteSettingAsync(string key);
}