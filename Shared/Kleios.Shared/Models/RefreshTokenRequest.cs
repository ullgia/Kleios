using System;
using System.Collections.Generic;

namespace Kleios.Shared.Models;

/// <summary>
/// Modello per la richiesta di refresh token
/// </summary>
public class RefreshTokenRequest
{
    public required string RefreshToken { get; set; }
}