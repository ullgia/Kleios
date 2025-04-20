namespace Kleios.Frontend.Infrastructure.Models;

/// <summary>
/// Modello per la risposta di errore
/// </summary>
public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}