using Microsoft.AspNetCore.Mvc;
using Kleios.Security.Authentication;
using Kleios.Shared.Models;
using Kleios.Shared;
using Kleios.Backend.Shared;

namespace Kleios.Backend.Authentication.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<Result<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        return await _authService.RegisterAsync(request);
    }

    [HttpPost("login")]
    public async Task<Result<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return Option<AuthResponse>.ValidationError("Dati di login non validi");
        }

        var option = await _authService.LoginAsync(request);
        return option;
    }

    [HttpPost("refresh")]
    public async Task<Result<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
        {
            return Option<AuthResponse>.ValidationError("Token di refresh non valido");
        }

        var option = await _authService.RefreshTokenAsync(request.RefreshToken);
        return option;
    }
}