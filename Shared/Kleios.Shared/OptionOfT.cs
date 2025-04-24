using System.Net;
using Kleios.Shared.Models;

namespace Kleios.Shared;

/// <summary>
/// Rappresenta un risultato di un'operazione che può avere successo con un valore o fallire.
/// </summary>
/// <typeparam name="T">Il tipo del valore in caso di successo.</typeparam>
public class Option<T> : Option
{
    private readonly T? _value;

    /// <summary>
    /// Valore associato al risultato dell'operazione.
    /// </summary>
    public T Value => IsSuccess 
        ? _value! 
        : throw new InvalidOperationException("Impossibile accedere al valore di un'operazione fallita");

    /// <summary>
    /// Crea una nuova istanza di Option<T> che rappresenta un successo con un valore.
    /// </summary>
    /// <param name="value">Valore associato al successo</param>
    /// <param name="statusCode">Codice di stato HTTP (default: 200 OK)</param>
    private Option(T value, HttpStatusCode statusCode = HttpStatusCode.OK) 
        : base(statusCode)
    {
        _value = value;
    }

    /// <summary>
    /// Crea una nuova istanza di Option<T> che rappresenta un fallimento.
    /// </summary>
    /// <param name="message">Messaggio di errore</param>
    /// <param name="statusCode">Codice di stato HTTP (default: 400 Bad Request)</param>
    private Option(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest) 
        : base(message, statusCode)
    {
        _value = default;
    }

    /// <summary>
    /// Crea un'istanza di Option<T> che rappresenta un successo con un valore.
    /// </summary>
    /// <param name="value">Valore associato al successo</param>
    /// <param name="statusCode">Codice di stato HTTP (default: 200 OK)</param>
    public static Option<T> Success(T value, HttpStatusCode statusCode = HttpStatusCode.OK) => new(value, statusCode);

    /// <summary>
    /// Crea un'istanza di Option<T> che rappresenta un fallimento.
    /// </summary>
    /// <param name="message">Messaggio di errore</param>
    /// <param name="statusCode">Codice di stato HTTP (default: 400 Bad Request)</param>
    public static new Option<T> Failure(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest) => new(message, statusCode);

    /// <summary>
    /// Crea un'istanza di Option<T> che rappresenta un errore "Not Found".
    /// </summary>
    /// <param name="message">Messaggio di errore</param>
    public static new Option<T> NotFound(string message = "Risorsa non trovata") => Failure(message, HttpStatusCode.NotFound);

    /// <summary>
    /// Crea un'istanza di Option<T> che rappresenta un errore "Unauthorized".
    /// </summary>
    /// <param name="message">Messaggio di errore</param>
    public static new Option<T> Unauthorized(string message = "Non autorizzato") => Failure(message, HttpStatusCode.Unauthorized);

    /// <summary>
    /// Crea un'istanza di Option<T> che rappresenta un errore "Forbidden".
    /// </summary>
    /// <param name="message">Messaggio di errore</param>
    public static new Option<T> Forbidden(string message = "Accesso vietato") => Failure(message, HttpStatusCode.Forbidden);

    /// <summary>
    /// Crea un'istanza di Option<T> che rappresenta un errore di validazione.
    /// </summary>
    /// <param name="message">Messaggio di errore</param>
    public static new Option<T> ValidationError(string message) => Failure(message, HttpStatusCode.BadRequest);

    /// <summary>
    /// Crea un'istanza di Option<T> che rappresenta un errore "Conflict".
    /// </summary>
    /// <param name="message">Messaggio di errore</param>
    public static new Option<T> Conflict(string message) => Failure(message, HttpStatusCode.Conflict);

    /// <summary>
    /// Crea un'istanza di Option<T> che rappresenta un errore interno del server.
    /// </summary>
    /// <param name="message">Messaggio di errore</param>
    public static new Option<T> ServerError(string message) => Failure(message, HttpStatusCode.InternalServerError);
    
    /// <summary>
    /// Conversione implicita da T a Option<T>.
    /// </summary>
    public static implicit operator Option<T>(T value) => Success(value);

    public static Option<T> TooManyRequests(string message) => Failure(message, HttpStatusCode.TooManyRequests);

}