// filepath: c:\Users\Giacomo\source\Kleios\Frontend\Infrastructure\Kleios.Frontend.Infrastructure\Services\LocalStorageService.cs
using System.Text.Json;
using Kleios.Frontend.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Kleios.Frontend.Infrastructure.Services;

/// <summary>
/// Implementazione del servizio per interagire con il localStorage del browser
/// </summary>
public class LocalStorageService : ILocalStorageService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly JsonSerializerOptions _jsonOptions;

    public LocalStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            // Durante il pre-rendering, restituisci il valore predefinito
            if (_jsRuntime is IJSInProcessRuntime == false)
            {
                return default;
            }
            
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
            
            if (string.IsNullOrEmpty(json))
                return default;

            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (InvalidOperationException)
        {
            // Gestisce l'eccezione durante il pre-rendering
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value)
    {
        try
        {
            // Salta l'operazione durante il pre-rendering
            if (_jsRuntime is IJSInProcessRuntime == false)
            {
                return;
            }
            
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, JsonSerializer.Serialize(value, _jsonOptions));
        }
        catch (InvalidOperationException)
        {
            // Ignora l'eccezione durante il pre-rendering
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            // Salta l'operazione durante il pre-rendering
            if (_jsRuntime is IJSInProcessRuntime == false)
            {
                return;
            }
            
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch (InvalidOperationException)
        {
            // Ignora l'eccezione durante il pre-rendering
        }
    }
}