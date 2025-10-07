using System.Security.Claims;
using Kleios.Backend.Authentication.Services;
using Microsoft.AspNetCore.Mvc;
using Kleios.Shared.Models;
using Kleios.Shared;
using Kleios.Backend.Shared;
using Kleios.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Kleios.Backend.Authentication.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IBackendAuthService _authService;

    public AuthController(IBackendAuthService authService)
    {
        _authService = authService;
    }


    [HttpPost("login")]
    public async Task<Result<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return Option<AuthResponse>.ValidationError("Dati di login non validi");
        }

        // Aggiunta raccolta dati client per sicurezza
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();

        return await _authService.LoginAsync(request, ipAddress, userAgent);
    }

    [HttpPost("refresh")]
    public async Task<Result<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
        {
            return Option<AuthResponse>.ValidationError("Token di refresh non valido");
        }

        // Raccolta dati client per sicurezza e tracciamento
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();

        var option = await _authService.RefreshTokenAsync(request.RefreshToken, ipAddress, userAgent);
        return option;
    }

   
    [HttpGet("security-stamp")]
    public async Task<Result<string>> GetSecurityStamp()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Option<string>.ValidationError("Utente non autenticato");
        }
        return  await _authService.GetSecurityStampAsync(userId);
    }
}