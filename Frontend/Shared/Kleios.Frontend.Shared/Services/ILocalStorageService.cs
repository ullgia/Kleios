// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Shared\Kleios.Frontend.Shared\Services\ILocalStorageService.cs
namespace Kleios.Frontend.Shared.Services;

/// <summary>
/// Interfaccia per il servizio di localStorage
/// </summary>
public interface ILocalStorageService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value);
    Task RemoveAsync(string key);
}