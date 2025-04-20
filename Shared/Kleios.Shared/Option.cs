using System.Net;

namespace Kleios.Shared;

/// <summary>
/// Rappresenta un risultato di un'operazione che può avere successo o fallire.
/// </summary>
public class Option
{
    private readonly bool _isSuccess;
    private readonly string? _message;
    private readonly HttpStatusCode _statusCode;

    /// <summary>
    /// Indica se l'operazione ha avuto successo.
    /// </summary>
    public bool IsSuccess => _isSuccess;
    
    /// <summary>
    /// Indica se l'operazione è fallita.
    /// </summary>
    public bool IsFailure => !_isSuccess;
    
    /// <summary>
    /// Messaggio associato al risultato dell'operazione.
    /// </summary>
    public string Message => _message ?? string.Empty;
    
    /// <summary>
    /// Codice di stato HTTP associato al risultato dell'operazione.
    /// </summary>
    public HttpStatusCode StatusCode => _statusCode;

    /// <summary>
    /// Crea una nuova istanza di Option che rappresenta un successo.
    /// </summary>
    /// <param name="statusCode">Codice di stato HTTP (default: 200 OK)</param>
    protected Option(HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _isSuccess = true;
        _statusCode = statusCode;
    }

    /// <summary>
    /// Crea una nuova istanza di Option che rappresenta un fallimento.
    /// </summary>
    /// <param name="message">Messaggio di errore</param>
    /// <param name="statusCode">Codice di stato HTTP (default: 400 Bad Request)</param>
    protected Option(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
    {
        _isSuccess = false;
        _message = message;
        _statusCode = statusCode;
    }

    /// <summary>
    /// Crea un'istanza di Option che rappresenta un successo.
    /// </summary>
    /// <param name="statusCode">Codice di stato HTTP (default: 200 OK)</param>
    public static Option Success(HttpStatusCode statusCode = HttpStatusCode.OK) => new(statusCode);

    /// <summary>
    /// Crea un'istanza di Option che rappresenta un fallimento.
    /// </summary>
    /// <param name="message">Messaggio di errore</param>
    /// <param name="statusCode">Codice di stato HTTP (default: 400 Bad Request)</param>
    public static Option Failure(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest) => new(message, statusCode);

    /// <summary>
    /// Crea un'istanza di Option che rappresenta un errore "Not Found".
    /// </summary>
    /// <param name="message">Messaggio di errore</param>
    public static Option NotFound(string message = "Risorsa non trovata") => Failure(message, HttpStatusCode.NotFound);

    /// <summary>
    /// Crea un'istanza di Option che rappresenta un errore "Unauthorized".
    /// </summary>
    /// <param name="message">Messaggio di errore</param>
    public static Option Unauthorized(string message = "Non autorizzato") => Failure(message, HttpStatusCode.Unauthorized);

    /// <summary>
    /// Crea un'istanza di Option che rappresenta un errore "Forbidden".
    /// </summary>
    /// <param name="message">Messaggio di errore</param>
    public static Option Forbidden(string message = "Accesso vietato") => Failure(message, HttpStatusCode.Forbidden);

    /// <summary>
    /// Crea un'istanza di Option che rappresenta un errore di validazione.
    /// </summary>
    /// <param name="message">Messaggio di errore</param>
    public static Option ValidationError(string message) => Failure(message, HttpStatusCode.BadRequest);

    /// <summary>
    /// Crea un'istanza di Option che rappresenta un errore "Conflict".
    /// </summary>
    /// <param name="message">Messaggio di errore</param>
    public static Option Conflict(string message) => Failure(message, HttpStatusCode.Conflict);

    /// <summary>
    /// Crea un'istanza di Option che rappresenta un errore interno del server.
    /// </summary>
    /// <param name="message">Messaggio di errore</param>
    public static Option ServerError(string message) => Failure(message, HttpStatusCode.InternalServerError);
}