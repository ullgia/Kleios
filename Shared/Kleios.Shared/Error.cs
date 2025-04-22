namespace Kleios.Shared;

/// <summary>
/// Rappresenta un errore generico dell'applicazione
/// </summary>
public class Error
{
    public string Code { get; }
    public string Message { get; }
    public ErrorSeverity Severity { get; }
    public Dictionary<string, object>? Metadata { get; }

    public Error(string code, string message, ErrorSeverity severity = ErrorSeverity.Error, Dictionary<string, object>? metadata = null)
    {
        Code = code;
        Message = message;
        Severity = severity;
        Metadata = metadata;
    }

    // Errori comuni predefiniti
    public static Error NotFound(string message) => new("NotFound", message, ErrorSeverity.Error);
    public static Error Validation(string message) => new("Validation", message, ErrorSeverity.Warning);
    public static Error Unauthorized(string message) => new("Unauthorized", message, ErrorSeverity.Error);
    public static Error Forbidden(string message) => new("Forbidden", message, ErrorSeverity.Error);
    public static Error Conflict(string message) => new("Conflict", message, ErrorSeverity.Error);
    public static Error Internal(string message) => new("Internal", message, ErrorSeverity.Critical);
}

/// <summary>
/// Livelli di severità per gli errori
/// </summary>
public enum ErrorSeverity
{
    Info,
    Warning,
    Error,
    Critical
}