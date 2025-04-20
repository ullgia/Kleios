using Kleios.Database.Context;
using Kleios.Database.Models;
using Kleios.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Kleios.Shared;

namespace Kleios.Security.Authentication;

/// <summary>
/// Interfaccia che definisce il servizio di autenticazione comune per tutti i backend
/// </summary>
public interface IAuthService
{
    Task<Option<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<Option<AuthResponse>> LoginAsync(LoginRequest request);
    Task<Option<AuthResponse>> RefreshTokenAsync(string refreshToken);
    Task<Option<List<UserResponse>>> GetUsersAsync();
}

/// <summary>
/// Implementazione del servizio di autenticazione
/// </summary>
public class AuthService : IAuthService
{
    private readonly KleiosDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IConfiguration _configuration;

    public AuthService(
        KleiosDbContext context, 
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IConfiguration configuration)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    public async Task<Option<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByNameAsync(request.Username);
        if (existingUser != null)
        {
            return Option<AuthResponse>.Conflict("Username already exists");
        }

        existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Option<AuthResponse>.Conflict("Email already exists");
        }

        // Find default user role
        var userRole = await _roleManager.FindByNameAsync("Utente");
        if (userRole == null)
        {
            return Option<AuthResponse>.ServerError("Default user role not found");
        }

        // Create new user
        var user = new ApplicationUser
        {
            UserName = request.Username,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = DateTime.UtcNow
        };

        // Create user with password (Identity automatically hashes the password)
        var Option = await _userManager.CreateAsync(user, request.Password);
        if (!Option.Succeeded)
        {
            return Option<AuthResponse>.ValidationError(string.Join(", ", Option.Errors.Select(e => e.Description)));
        }
        
        // Assign default role to user
        if (userRole.Name != null)
        {
            await _userManager.AddToRoleAsync(user, userRole.Name);
        }

        // Generate JWT token
        var token = await GenerateJwtToken(user);
        
        // Generate refresh token
        var refreshToken = GenerateRefreshToken(user);
        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        var response = new AuthResponse
        {
            Token = token,
            RefreshToken = refreshToken.Token,
            Username = user.UserName ?? string.Empty,
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            Role = userRole.Name ?? "Utente", // For backward compatibility
            Roles = new List<string> { userRole.Name ?? "Utente" }, // Include all roles (just one for new users)
            Expiration = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JWT:ExpiryInMinutes"] ?? "60"))
        };

        return Option<AuthResponse>.Success(response);
    }

    public async Task<Option<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var allUsers = _userManager.Users.ToList();

        // Find user by username
        var user = await _userManager.FindByNameAsync(request.Username);
        if (user == null)
        {
            return Option<AuthResponse>.Unauthorized("Invalid username or password");
        }

        // Verify password using Identity's password hasher
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            return Option<AuthResponse>.Unauthorized("Invalid username or password");
        }

        // Get user roles
        var roles = await _userManager.GetRolesAsync(user);
        
        // Use the first role as the primary role for backward compatibility
        string primaryRole = roles.FirstOrDefault() ?? "Utente";

        // Generate JWT token
        var token = await GenerateJwtToken(user);
        
        // Generate refresh token
        var refreshToken = GenerateRefreshToken(user);
        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        var response = new AuthResponse
        {
            Token = token,
            RefreshToken = refreshToken.Token,
            Username = user.UserName ?? string.Empty,
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            Role = primaryRole, // Set the first role as primary for backward compatibility
            Roles = roles.ToList(), // Include all roles
            Expiration = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JWT:ExpiryInMinutes"] ?? "60"))
        };

        return Option<AuthResponse>.Success(response);
    }

    /// <summary>
    /// Aggiorna il token di accesso utilizzando un refresh token
    /// </summary>
    public async Task<Option<AuthResponse>> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            // Cerchiamo il refresh token nel database
            var storedToken = await _context.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == refreshToken);
            
            // Verifica che il token esista e sia valido
            if (storedToken == null)
            {
                return Option<AuthResponse>.Unauthorized("Refresh token non valido");
            }
            
            // Verifica che il token non sia scaduto o revocato
            if (storedToken.ExpiryDate < DateTime.UtcNow || storedToken.IsRevoked)
            {
                return Option<AuthResponse>.Unauthorized("Refresh token scaduto o revocato");
            }
            
            // Revoca il token corrente (sicurezza: rotazione dei token)
            storedToken.IsRevoked = true;
            _context.RefreshTokens.Update(storedToken);
            
            // Genera un nuovo refresh token
            var newRefreshToken = GenerateRefreshToken(storedToken.User);
            await _context.RefreshTokens.AddAsync(newRefreshToken);
            await _context.SaveChangesAsync();
            
            // Genera un nuovo token JWT
            var accessToken = await GenerateJwtToken(storedToken.User);
            
            // Prepara la risposta
            var response = new AuthResponse
            {
                Token = accessToken,
                RefreshToken = newRefreshToken.Token,
                Username = storedToken.User.UserName ?? string.Empty,
                UserId = storedToken.User.Id,
                Email = storedToken.User.Email ?? string.Empty,
                Role = (await _userManager.GetRolesAsync(storedToken.User)).FirstOrDefault() ?? "Utente",
                Roles = (await _userManager.GetRolesAsync(storedToken.User)).ToList(),
                Expiration = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JWT:ExpiryInMinutes"] ?? "60"))
            };
            
            return Option<AuthResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Option<AuthResponse>.ServerError($"Errore durante il refresh del token: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Genera un nuovo refresh token per un utente
    /// </summary>
    private RefreshToken GenerateRefreshToken(ApplicationUser user)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = Guid.NewGuid().ToString() + "-" + Guid.NewGuid().ToString(),
            ExpiryDate = DateTime.UtcNow.AddDays(7), // I refresh token durano 7 giorni
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task<string> GenerateJwtToken(ApplicationUser user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"] ?? "Kleios_JWT_Secret_Key_For_Auth_At_Least_32_Characters"));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Get user roles
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Name, user.UserName ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        
        // Add all roles as claims
        if (roles.Any())
        {
            foreach (var role in roles)
            {
                if (!string.IsNullOrEmpty(role))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }
            
            // Add permissions for roles as claims
            var rolePermissions = await _context.RolePermissions
                .Include(rp => rp.Permission)
                .Where(rp => rp.Role.Name != null && roles.Contains(rp.Role.Name))
                .Select(rp => rp.Permission)
                .ToListAsync();

            foreach (var permission in rolePermissions)
            {
                if (permission.SystemName != null)
                {
                    claims.Add(new Claim("Permission", permission.SystemName));
                }
            }
        }
        else
        {
            // Fallback to default role if no roles found
            claims.Add(new Claim(ClaimTypes.Role, "Utente"));
        }

        var token = new JwtSecurityToken(
            issuer: _configuration["JWT:ValidIssuer"],
            audience: _configuration["JWT:ValidAudience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JWT:ExpiryInMinutes"] ?? "60")),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Recupera la lista di tutti gli utenti registrati
    /// </summary>
    public async Task<Option<List<UserResponse>>> GetUsersAsync()
    {
        try
        {
            // Recupera tutti gli utenti dal database
            var users = await _userManager.Users.ToListAsync();
            var userResponses = new List<UserResponse>();
            
            foreach (var user in users)
            {
                // Recupera i ruoli dell'utente
                var roles = await _userManager.GetRolesAsync(user);
                
                userResponses.Add(new UserResponse
                {
                    Id = user.Id,
                    Username = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Roles = roles.ToList(),
                    CreatedAt = user.CreatedAt,
                    IsActive = user.LockoutEnd == null || user.LockoutEnd < DateTimeOffset.UtcNow
                });
            }
            
            return Option<List<UserResponse>>.Success(userResponses);
        }
        catch (Exception ex)
        {
            return Option<List<UserResponse>>.ServerError($"Errore durante il recupero degli utenti: {ex.Message}");
        }
    }
}