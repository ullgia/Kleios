using Kleios.Database.Context;
using Kleios.Database.Models;
using Kleios.Shared.Models;
using Kleios.Backend.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kleios.Backend.SystemAdmin.Services;

public interface IPasswordPolicyService
{
    Task<PasswordPolicyDto> GetPasswordPolicyAsync();
    Task<PasswordPolicyDto> UpdatePasswordPolicyAsync(PasswordPolicyDto policy);
    Task<PasswordValidationResult> ValidatePasswordAsync(string password);
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task<bool> ForceChangePasswordAsync(Guid userId, string newPassword);
    Task<bool> SendPasswordResetEmailAsync(string email);
    Task<bool> ResetPasswordAsync(ConfirmResetPasswordRequest request);
    Task<UserPasswordStatusDto> GetUserPasswordStatusAsync(Guid userId);
    Task<IEnumerable<UserPasswordStatusDto>> GetExpiredPasswordsAsync();
}

public class PasswordPolicyService : IPasswordPolicyService
{
    private readonly KleiosDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PasswordPolicyService> _logger;
    private readonly ISettingsService _settingsService;
    private readonly IEmailService _emailService;

    public PasswordPolicyService(
        KleiosDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<PasswordPolicyService> logger,
        ISettingsService settingsService,
        IEmailService emailService)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
        _settingsService = settingsService;
        _emailService = emailService;
    }

    public async Task<PasswordPolicyDto> GetPasswordPolicyAsync()
    {
        var policy = new PasswordPolicyDto
        {
            MinimumLength = await _settingsService.GetSettingValueAsync<int?>("Security:Password:MinimumLength") ?? 8,
            MaximumLength = await _settingsService.GetSettingValueAsync<int?>("Security:Password:MaximumLength") ?? 128,
            RequireUppercase = await _settingsService.GetSettingValueAsync<bool?>("Security:Password:RequireUppercase") ?? true,
            RequireLowercase = await _settingsService.GetSettingValueAsync<bool?>("Security:Password:RequireLowercase") ?? true,
            RequireDigit = await _settingsService.GetSettingValueAsync<bool?>("Security:Password:RequireDigit") ?? true,
            RequireSpecialCharacter = await _settingsService.GetSettingValueAsync<bool?>("Security:Password:RequireSpecialCharacter") ?? true,
            PasswordHistorySize = await _settingsService.GetSettingValueAsync<int?>("Security:Password:HistorySize") ?? 5,
            PasswordExpirationDays = await _settingsService.GetSettingValueAsync<int?>("Security:Password:ExpirationDays") ?? 90,
            MaxFailedAccessAttempts = await _settingsService.GetSettingValueAsync<int?>("Security:Account:MaxFailedAccessAttempts") ?? 5,
            LockoutDurationMinutes = await _settingsService.GetSettingValueAsync<int?>("Security:Account:LockoutDurationMinutes") ?? 30
        };

        return policy;
    }

    public async Task<PasswordPolicyDto> UpdatePasswordPolicyAsync(PasswordPolicyDto policy)
    {
        await _settingsService.UpdateSettingAsync("Security:Password:MinimumLength", policy.MinimumLength.ToString());
        await _settingsService.UpdateSettingAsync("Security:Password:MaximumLength", policy.MaximumLength.ToString());
        await _settingsService.UpdateSettingAsync("Security:Password:RequireUppercase", policy.RequireUppercase.ToString());
        await _settingsService.UpdateSettingAsync("Security:Password:RequireLowercase", policy.RequireLowercase.ToString());
        await _settingsService.UpdateSettingAsync("Security:Password:RequireDigit", policy.RequireDigit.ToString());
        await _settingsService.UpdateSettingAsync("Security:Password:RequireSpecialCharacter", policy.RequireSpecialCharacter.ToString());
        await _settingsService.UpdateSettingAsync("Security:Password:HistorySize", policy.PasswordHistorySize.ToString());
        await _settingsService.UpdateSettingAsync("Security:Password:ExpirationDays", policy.PasswordExpirationDays.ToString());
        await _settingsService.UpdateSettingAsync("Security:Account:MaxFailedAccessAttempts", policy.MaxFailedAccessAttempts.ToString());
        await _settingsService.UpdateSettingAsync("Security:Account:LockoutDurationMinutes", policy.LockoutDurationMinutes.ToString());

        _logger.LogInformation("Password policy aggiornata");
        return policy;
    }

    public async Task<PasswordValidationResult> ValidatePasswordAsync(string password)
    {
        var result = new PasswordValidationResult();
        var policy = await GetPasswordPolicyAsync();

        if (password.Length < policy.MinimumLength)
        {
            result.Errors.Add($"La password deve contenere almeno {policy.MinimumLength} caratteri");
        }

        if (password.Length > policy.MaximumLength)
        {
            result.Errors.Add($"La password non può superare {policy.MaximumLength} caratteri");
        }

        if (policy.RequireUppercase && !password.Any(char.IsUpper))
        {
            result.Errors.Add("La password deve contenere almeno una lettera maiuscola");
        }

        if (policy.RequireLowercase && !password.Any(char.IsLower))
        {
            result.Errors.Add("La password deve contenere almeno una lettera minuscola");
        }

        if (policy.RequireDigit && !password.Any(char.IsDigit))
        {
            result.Errors.Add("La password deve contenere almeno un numero");
        }

        if (policy.RequireSpecialCharacter && !password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            result.Errors.Add("La password deve contenere almeno un carattere speciale");
        }

        // Calcola il punteggio di forza
        result.StrengthScore = CalculatePasswordStrength(password, policy);
        result.StrengthLevel = result.StrengthScore switch
        {
            < 40 => "Debole",
            < 60 => "Discreta",
            < 80 => "Buona",
            < 95 => "Forte",
            _ => "Eccellente"
        };

        result.IsValid = !result.Errors.Any();
        return result;
    }

    private int CalculatePasswordStrength(string password, PasswordPolicyDto policy)
    {
        int score = 0;

        // Lunghezza
        if (password.Length >= policy.MinimumLength) score += 20;
        if (password.Length >= policy.MinimumLength + 4) score += 10;
        if (password.Length >= policy.MinimumLength + 8) score += 10;

        // Complessità
        if (password.Any(char.IsUpper)) score += 15;
        if (password.Any(char.IsLower)) score += 15;
        if (password.Any(char.IsDigit)) score += 15;
        if (password.Any(ch => !char.IsLetterOrDigit(ch))) score += 15;

        // Varietà di caratteri
        var uniqueChars = password.Distinct().Count();
        if (uniqueChars >= password.Length * 0.5) score += 10;

        return Math.Min(score, 100);
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("Tentativo di cambio password per utente non esistente: {UserId}", userId);
            return false;
        }

        // Valida la nuova password
        var validation = await ValidatePasswordAsync(request.NewPassword);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Password non valida per l'utente {UserId}", userId);
            return false;
        }

        // Verifica la password corrente
        if (!await _userManager.CheckPasswordAsync(user, request.CurrentPassword))
        {
            _logger.LogWarning("Password corrente errata per l'utente {UserId}", userId);
            return false;
        }

        // Cambia la password
        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (result.Succeeded)
        {
            user.LastPasswordChangeDate = DateTime.UtcNow;
            user.MustChangePassword = false;
            await _userManager.UpdateAsync(user);
            _logger.LogInformation("Password cambiata con successo per l'utente {UserId}", userId);
            return true;
        }

        _logger.LogError("Errore durante il cambio password per l'utente {UserId}: {Errors}", 
            userId, string.Join(", ", result.Errors.Select(e => e.Description)));
        return false;
    }

    public async Task<bool> ForceChangePasswordAsync(Guid userId, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("Tentativo di reset password per utente non esistente: {UserId}", userId);
            return false;
        }

        // Rimuovi la password corrente
        await _userManager.RemovePasswordAsync(user);

        // Aggiungi la nuova password
        var result = await _userManager.AddPasswordAsync(user, newPassword);
        if (result.Succeeded)
        {
            user.LastPasswordChangeDate = DateTime.UtcNow;
            user.MustChangePassword = true;
            await _userManager.UpdateAsync(user);
            _logger.LogInformation("Password forzata con successo per l'utente {UserId}", userId);
            return true;
        }

        _logger.LogError("Errore durante il reset password per l'utente {UserId}: {Errors}", 
            userId, string.Join(", ", result.Errors.Select(e => e.Description)));
        return false;
    }

    public async Task<bool> SendPasswordResetEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            // Non rivelare se l'email esiste o meno per sicurezza
            _logger.LogInformation("Richiesta reset password per email non esistente: {Email}", email);
            return true;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        
        // Invia email con il token tramite EmailService
        var emailSent = await _emailService.SendPasswordResetEmailAsync(email, user.UserName ?? email, token);
        
        if (emailSent)
        {
            _logger.LogInformation("Email di reset password inviata con successo per l'utente {UserId}", user.Id);
        }
        else
        {
            _logger.LogWarning("Impossibile inviare l'email di reset password per l'utente {UserId}", user.Id);
        }
        
        return emailSent;
    }

    public async Task<bool> ResetPasswordAsync(ConfirmResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Tentativo di reset password per email non esistente: {Email}", request.Email);
            return false;
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (result.Succeeded)
        {
            user.LastPasswordChangeDate = DateTime.UtcNow;
            user.MustChangePassword = false;
            await _userManager.UpdateAsync(user);
            _logger.LogInformation("Password resettata con successo per l'utente {UserId}", user.Id);
            return true;
        }

        _logger.LogError("Errore durante il reset password per l'utente {UserId}: {Errors}", 
            user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
        return false;
    }

    public async Task<UserPasswordStatusDto> GetUserPasswordStatusAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"Utente non trovato: {userId}");
        }

        var policy = await GetPasswordPolicyAsync();
        var expirationDate = user.LastPasswordChangeDate?.AddDays(policy.PasswordExpirationDays);
        var daysUntilExpiration = expirationDate.HasValue 
            ? (int)(expirationDate.Value - DateTime.UtcNow).TotalDays 
            : policy.PasswordExpirationDays;

        return new UserPasswordStatusDto
        {
            UserId = userId,
            LastPasswordChangeDate = user.LastPasswordChangeDate,
            DaysUntilExpiration = daysUntilExpiration,
            IsExpired = daysUntilExpiration <= 0,
            MustChangePassword = user.MustChangePassword,
            FailedAccessAttempts = user.AccessFailedCount,
            IsLockedOut = await _userManager.IsLockedOutAsync(user),
            LockoutEnd = user.LockoutEnd?.UtcDateTime
        };
    }

    public async Task<IEnumerable<UserPasswordStatusDto>> GetExpiredPasswordsAsync()
    {
        var policy = await GetPasswordPolicyAsync();
        var expirationThreshold = DateTime.UtcNow.AddDays(-policy.PasswordExpirationDays);

        var users = await _context.Users
            .Where(u => u.LastPasswordChangeDate < expirationThreshold || u.LastPasswordChangeDate == null)
            .ToListAsync();

        var result = new List<UserPasswordStatusDto>();
        foreach (var user in users)
        {
            result.Add(await GetUserPasswordStatusAsync(user.Id));
        }

        return result;
    }
}
