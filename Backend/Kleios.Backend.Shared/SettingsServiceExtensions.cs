using Kleios.Shared;
using Kleios.Shared.Settings;
using System.Globalization;

namespace Kleios.Backend.Shared;

/// <summary>
/// Extension methods for ISettingsService to simplify getting typed setting values
/// </summary>
public static class SettingsServiceExtensions
{
    /// <summary>
    /// Gets a setting value by key and converts it to the specified type
    /// Returns null if the setting doesn't exist or conversion fails
    /// </summary>
    public static async Task<T?> GetSettingValueAsync<T>(this ISettingsService settingsService, string key)
    {
        var result = await settingsService.GetSettingByKeyAsync(key);
        
        if (!result.IsSuccess || result.Value == null)
        {
            return default;
        }

        var setting = result.Value;
        var value = setting.Value;

        if (string.IsNullOrEmpty(value))
        {
            return default;
        }

        try
        {
            var targetType = typeof(T);
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlyingType == typeof(string))
            {
                return (T)(object)value;
            }
            else if (underlyingType == typeof(int))
            {
                return (T)(object)int.Parse(value, CultureInfo.InvariantCulture);
            }
            else if (underlyingType == typeof(bool))
            {
                return (T)(object)bool.Parse(value);
            }
            else if (underlyingType == typeof(double))
            {
                return (T)(object)double.Parse(value, CultureInfo.InvariantCulture);
            }
            else if (underlyingType == typeof(decimal))
            {
                return (T)(object)decimal.Parse(value, CultureInfo.InvariantCulture);
            }
            else if (underlyingType == typeof(DateTime))
            {
                return (T)(object)DateTime.Parse(value, CultureInfo.InvariantCulture);
            }
            else if (underlyingType.IsEnum)
            {
                return (T)Enum.Parse(underlyingType, value, true);
            }
            else
            {
                // For other types, try using Convert.ChangeType
                return (T)Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
            }
        }
        catch (Exception)
        {
            return default;
        }
    }
}
