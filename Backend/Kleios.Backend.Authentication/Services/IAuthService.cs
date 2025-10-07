using Kleios.Backend.Shared;
using Kleios.Shared;
using Kleios.Shared.Models;
using Kleios.Database.Models;
using Kleios.Database.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Kleios.Shared.Authorization;
using Kleios.Shared.Settings;

namespace Kleios.Backend.Authentication.Services;

/// <summary>
/// Interfaccia per il servizio di autenticazione backend
/// Nota: Rinominata da IAuthService per evitare conflitti con Frontend.IFrontendAuthService
/// </summary>
public interface IBackendAuthService
{
    Task<Option<AuthResponse>> LoginAsync(LoginRequest request, string ipAddress, string userAgent);
    Task<Option<AuthResponse>> RefreshTokenAsync(string requestRefreshToken, string ipAddress, string userAgent);
    Task<Option<string>> GetSecurityStampAsync(string userId);
}

public class AuthService : IBackendAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly KleiosDbContext _dbContext;
    private readonly ILogger<AuthService> _logger;
    private readonly JwtSettingsModel _jwtSettings;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        KleiosDbContext dbContext,
        ILogger<AuthService> logger,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _logger = logger;
        
        // Ottiene le impostazioni JWT dalla configurazione
        _jwtSettings = new JwtSettingsModel();
        configuration.GetSection("JwtSettings").Bind(_jwtSettings);
    }

    /// <summary>
    /// Gestisce il login di un utente e genera i token di autenticazione
    /// </summary>
    public async Task<Option<AuthResponse>> LoginAsync(LoginRequest request, string ipAddress, string userAgent)
    {
        try
        {
            // Validazione input
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return Option<AuthResponse>.ValidationError("Username e password sono obbligatori");
            }

            // Trova l'utente
            var user = await _userManager.FindByNameAsync(request.Username);
            if (user == null)
            {
                // Potremmo anche cercare per email se il sistema lo supporta
                user = await _userManager.FindByEmailAsync(request.Username);
            }

            // Verifica se l'utente esiste
            if (user == null)
            {
                _logger.LogWarning("Tentativo di login fallito per username non esistente: {Username}", request.Username);
                return Option<AuthResponse>.NotFound("Credenziali non valide");
            }

            // Verifica password
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!isPasswordValid)
            {
                _logger.LogWarning("Password errata per l'utente: {Username}", request.Username);
                return Option<AuthResponse>.Unauthorized("Credenziali non valide");
            }

            // Verifica che l'account sia attivo
            if (!user.EmailConfirmed)
            {
                _logger.LogWarning("Tentativo di login con account non confermato: {Username}", request.Username);
                return Option<AuthResponse>.Unauthorized("Account non confermato. Verifica l'email per attivare l'account.");
            }

            // Ottieni i ruoli dell'utente
            var userRoles = await _userManager.GetRolesAsync(user);
            
            // Genera i token (JWT e Refresh Token)
            var jwtToken = await GenerateJwtTokenAsync(user, userRoles);
            var refreshToken = GenerateRefreshToken(ipAddress, userAgent);
            
            // Salva il refresh token nel database
            refreshToken.UserId = user.Id;
            refreshToken.JwtId = jwtToken.Id;
            
            _dbContext.RefreshTokens.Add(refreshToken);
            await _dbContext.SaveChangesAsync();

            // Crea e restituisci la risposta di autenticazione
            var response = new AuthResponse
            {
                UserId = user.Id,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                RefreshToken = refreshToken.Token,
                Expiration = jwtToken.ValidTo,
                Role = userRoles.FirstOrDefault() ?? string.Empty, // Mantiene la compatibilità all'indietro
                Roles = userRoles.ToList(),
                SecurityStamp = user.SecurityStamp ?? string.Empty
            };

            _logger.LogInformation("Login riuscito per l'utente: {Username}", user.UserName);
            return Option<AuthResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il login: {Message}", ex.Message);
            return Option<AuthResponse>.ServerError("Si è verificato un errore durante il login");
        }
    }

    /// <summary>
    /// Rinnova i token utilizzando un refresh token valido
    /// </summary>
    public async Task<Option<AuthResponse>> RefreshTokenAsync(string requestRefreshToken, string ipAddress, string userAgent)
    {
        try
        {
            // Trova il refresh token nel database
            var refreshToken = await _dbContext.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == requestRefreshToken);

            // Verifica se il token esiste
            if (refreshToken == null)
            {
                _logger.LogWarning("Refresh token non trovato: {Token}", requestRefreshToken);
                return Option<AuthResponse>.NotFound("Token di refresh non valido");
            }

            // Verifica se il token è scaduto
            if (refreshToken.ExpiryDate < DateTime.UtcNow)
            {
                _logger.LogWarning("Refresh token scaduto: {Token}", requestRefreshToken);
                return Option<AuthResponse>.Unauthorized("Token di refresh scaduto");
            }

            // Verifica se il token è stato revocato
            if (refreshToken.IsRevoked)
            {
                _logger.LogWarning("Tentativo di utilizzo di un refresh token revocato: {Token}", requestRefreshToken);
                return Option<AuthResponse>.Unauthorized("Token di refresh revocato");
            }

            // Ottieni l'utente associato al token
            var user = refreshToken.User;
            if (user == null)
            {
                _logger.LogWarning("Utente non trovato per il refresh token: {Token}", requestRefreshToken);
                return Option<AuthResponse>.NotFound("Utente non trovato");
            }

            // Ottieni i ruoli dell'utente
            var userRoles = await _userManager.GetRolesAsync(user);

            // Crea un nuovo JWT
            var newJwtToken = await GenerateJwtTokenAsync(user, userRoles);

            // Crea un nuovo refresh token
            var newRefreshToken = GenerateRefreshToken(ipAddress, userAgent);
            newRefreshToken.UserId = user.Id;
            newRefreshToken.JwtId = newJwtToken.Id;

            // Aggiorna il vecchio refresh token come revocato
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevocationReason = "Sostituito da nuovo token";
            refreshToken.LastUsedByIp = ipAddress;
            refreshToken.LastUsedAt = DateTime.UtcNow;
            refreshToken.UseCount++;

            // Salva il nuovo refresh token
            _dbContext.RefreshTokens.Add(newRefreshToken);
            await _dbContext.SaveChangesAsync();

            // Crea e restituisci la risposta di autenticazione
            var response = new AuthResponse
            {
                UserId = user.Id,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Token = new JwtSecurityTokenHandler().WriteToken(newJwtToken),
                RefreshToken = newRefreshToken.Token,
                Expiration = newJwtToken.ValidTo,
                Role = userRoles.FirstOrDefault() ?? string.Empty,
                Roles = userRoles.ToList(),
                SecurityStamp = user.SecurityStamp ?? string.Empty
            };

            _logger.LogInformation("Refresh token completato con successo per l'utente: {Username}", user.UserName);
            return Option<AuthResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il refresh del token: {Message}", ex.Message);
            return Option<AuthResponse>.ServerError("Si è verificato un errore durante il refresh del token");
        }
    }

    /// <summary>
    /// Ottiene il security stamp di un utente per validare le sessioni
    /// </summary>
    public async Task<Option<string>> GetSecurityStampAsync(string userId)
    {
        try
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                return Option<string>.ValidationError("ID utente non valido");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Utente non trovato durante la richiesta del security stamp: {UserId}", userId);
                return Option<string>.NotFound("Utente non trovato");
            }

            var securityStamp = await _userManager.GetSecurityStampAsync(user);
            return Option<string>.Success(securityStamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero del security stamp: {Message}", ex.Message);
            return Option<string>.ServerError("Si è verificato un errore durante il recupero del security stamp");
        }
    }

    #region Helper Methods

    /// <summary>
    /// Genera un token JWT con i claim appropriati
    /// </summary>
    private async Task<JwtSecurityToken> GenerateJwtTokenAsync(ApplicationUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim("security_stamp", user.SecurityStamp ?? string.Empty)
        };

        // Aggiungi i ruoli come claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            
            // Aggiungi i permessi associati a questo ruolo
            var roleEntity = await _roleManager.FindByNameAsync(role);
            if (roleEntity != null)
            {
                var permissions = await _dbContext.RolePermissions
                    .Where(rp => rp.RoleId == roleEntity.Id)
                    .Include(rp => rp.Permission)
                    .Select(rp => rp.Permission)
                    .ToListAsync();
                
                foreach (var permission in permissions)
                {
                    claims.Add(new Claim(ApplicationClaimTypes.Permission, permission.SystemName));
                }
            }
        }

        // Utilizza _jwtSettings per ottenere le credenziali di firma
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Crea il token utilizzando le impostazioni da _jwtSettings
        var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.TokenValidityInMinutes);
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return token;
    }

    /// <summary>
    /// Genera un nuovo refresh token
    /// </summary>
    private RefreshToken GenerateRefreshToken(string ipAddress, string userAgent)
    {
        // Crea un nuovo token casuale sicuro
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var token = Convert.ToBase64String(randomBytes);

        // Crea l'entità RefreshToken
        var refreshToken = new RefreshToken
        {
            Token = token,
            ExpiryDate = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenValidityInDays),
            CreatedByIp = ipAddress,
            UserAgent = userAgent
        };

        return refreshToken;
    }

    #endregion
}