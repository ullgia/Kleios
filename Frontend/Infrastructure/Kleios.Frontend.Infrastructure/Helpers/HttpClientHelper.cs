using Kleios.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;

namespace Kleios.Frontend.Infrastructure.Helpers;

public static class HttpClientHelper
{
    public static async Task<Option<T>> PostAsJson<T>(this HttpClient client, string requestUri, object? value = null)
    {
        try
        {

            var response = await client.PostAsJsonAsync(requestUri, value);
            if (!response.IsSuccessStatusCode)
            {
                return await ManageError<T>(response);
            }
            var result = await response.Content.ReadFromJsonAsync<T>();
            return result ?? Option<T>.ServerError("Nessun contenuto");
        }
        catch (Exception e)
        {
            return Option<T>.ServerError(e.Message);
        }
    }

    public static async Task<Option> PatchAsJson(this HttpClient client, string requestUri, object? value)
    {
        try
        {
            var response = await client.PatchAsJsonAsync(requestUri, value);
            if (!response.IsSuccessStatusCode) return await ManageError(response);
            return Option.Success();
        }
        catch (Exception e)
        {
            return Option.ServerError(e.Message);
        }
    }

    private static async Task<Option> ManageError(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
        {
            return Option.Failure("", response.StatusCode);
        }

        if (content.StartsWith("{") && content.EndsWith("}"))
        {
            var errorRestul = JsonSerializer.Deserialize<ProblemDetails>(content);
            if (errorRestul is null)
            {
                var strMessage = await response.Content.ReadAsStringAsync();
                return Option.Failure(strMessage,response.StatusCode);
            }

            return Option.ServerError(errorRestul.Detail ?? "Unhandled Error");
        }
        return Option.ServerError(content);
    }

    private static async Task<Option<T>> ManageError<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
        {
            return Option<T>.Failure("", response.StatusCode);
        }

        if (content.StartsWith("{") && content.EndsWith("}"))
        {
            var errorRestul = JsonSerializer.Deserialize<ProblemDetails>(content);
            if (errorRestul is null)
            {
                var strMessage = await response.Content.ReadAsStringAsync();
                return Option<T>.Failure(strMessage, response.StatusCode);
            }

            return Option<T>.Failure(errorRestul.Detail ?? "Unhandled Error", response.StatusCode);
        }
        return Option<T>.Failure(content, response.StatusCode);

    }

    public static async Task<Option> PostAsJson(this HttpClient client, string requestUri, object? value)
    {
        try
        {
            var response = await client.PostAsJsonAsync(requestUri, value);
            if (!response.IsSuccessStatusCode) return await ManageError(response);
            return Option.Success();
        }
        catch (Exception e)
        {
            return Option.ServerError(e.Message);
        }
    }

    public static async Task<Option> PutAsJson(this HttpClient client, string requestUri, object? value)
    {
        try
        {
            var response = await client.PutAsJsonAsync(requestUri, value);
            if (!response.IsSuccessStatusCode) return await ManageError(response);

            return Option.Success(); 
        }
        catch (Exception e)
        {
            return Option.ServerError(e.Message);
        }
    }

    public static async Task<Option<T>> PutAsJson<T>(this HttpClient client, string requestUri, object? value)
    {
        try
        {
            var response = await client.PutAsJsonAsync(requestUri, value);
            if (!response.IsSuccessStatusCode) return await ManageError<T>(response);
            if (typeof(T) == typeof(string))
            {
                return (T)Convert.ChangeType(await response.Content.ReadAsStringAsync(), typeof(T));
            }
            var result = await response.Content.ReadFromJsonAsync<T>();
            return result ?? Option<T>.ServerError("Nessun contenuto");
        }
        catch (Exception e)
        {
            return Option<T>.Failure(e.Message, HttpStatusCode.InternalServerError);
        }
    }

    public static async Task<Option> Delete(this HttpClient client, string requestUri)
    {
        try
        {
            var response = await client.DeleteAsync(requestUri);
            if (!response.IsSuccessStatusCode) return await ManageError(response);

            return Option.Success();
        }
        catch (Exception e)
        {
            return Option.Failure(e.Message, HttpStatusCode.InternalServerError);
        }
    }

    public static async Task<Option> Get(this HttpClient client, string requestUri)
    {
        try
        {
            var response = await client.GetAsync(requestUri);
            if (!response.IsSuccessStatusCode) return await ManageError(response);

            return Option.Success();
        }
        catch (Exception e)
        {
            return Option.Failure(e.Message, HttpStatusCode.InternalServerError);
        }
    }

    public static async Task<Option<T>> Get<T>(this HttpClient client, string requestUri)
    {
        HttpResponseMessage? response = null;
        try
        {
            response = await client.GetAsync(requestUri);
            if (!response.IsSuccessStatusCode) return await ManageError<T>(response);
            if (typeof(T) == typeof(string))
            {
                return (T)Convert.ChangeType(await response.Content.ReadAsStringAsync(), typeof(T));
            }
            var result = await response.Content.ReadFromJsonAsync<T>();
            if (result is null)
            {
                var message = await response.Content.ReadAsStringAsync();
                throw new InvalidCastException(message);
            }

            return result;
        }
        catch (InvalidOperationException e)
        {
            if (response is null)
                return Option<T>.Failure(e.Message, HttpStatusCode.InternalServerError);

            var strResult = await response.Content.ReadAsStringAsync();
            return Option<T>.Failure($"Impossibile convertire in {typeof(T)} la stringa {strResult}", HttpStatusCode.InternalServerError);
        }
        catch (Exception e)
        {
            return Option<T>.Failure(e.Message, HttpStatusCode.InternalServerError);
        }
    }

    public static Task<Option<T>> Get<T>(this HttpClient client, string query, object request)
    {
        return client.Get<T>($"{query}?{request.ToQueryString()}");
    }

    public static string ToQueryString<T>(this T? request)
    {
        if (request == null)
        {
            return string.Empty;
        }

        var properties = request.GetType()
            .GetProperties()
            .Where(p => p.GetValue(request, null) != null)
            .ToDictionary(p => p.Name, p => p.GetValue(request, null)?.ToString()!);

        var queryString = string.Join("&", properties.Select(p => $"{p.Key}={WebUtility.UrlEncode(p.Value)}"));

        return queryString;
    }
}
