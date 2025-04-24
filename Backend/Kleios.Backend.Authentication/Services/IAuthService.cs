using Kleios.Backend.Shared;
using Kleios.Shared;
using Kleios.Shared.Models;

namespace Kleios.Backend.Authentication.Services;

public interface IAuthService
{
    Task<Option<AuthResponse>> LoginAsync(LoginRequest request, string ipAddress, string userAgent);
    Task<Option<AuthResponse>> RefreshTokenAsync(string requestRefreshToken, string ipAddress, string userAgent);
    Task<Option<string>> GetSecurityStampAsync(string userId);
}

public class AuthService : IAuthService
{
    public Task<Option<AuthResponse>> LoginAsync(LoginRequest request, string ipAddress, string userAgent)
    {
        throw new NotImplementedException();
    }

    public Task<Option<AuthResponse>> RefreshTokenAsync(string requestRefreshToken, string ipAddress, string userAgent)
    {
        throw new NotImplementedException();
    }

    public Task<Option<string>> GetSecurityStampAsync(string userId)
    {
        throw new NotImplementedException();
    }
}