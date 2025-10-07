namespace Kleios.Shared.Models;

/// <summary>
/// Configurazione delle policy per le password
/// </summary>
public class PasswordPolicyDto
{
    public int MinimumLength { get; set; } = 8;
    public int MaximumLength { get; set; } = 128;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireSpecialCharacter { get; set; } = true;
    public int PasswordHistorySize { get; set; } = 5;
    public int PasswordExpirationDays { get; set; } = 90;
    public int MaxFailedAccessAttempts { get; set; } = 5;
    public int LockoutDurationMinutes { get; set; } = 30;
}

/// <summary>
/// Risultato della validazione di una password
/// </summary>
public class PasswordValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public int StrengthScore { get; set; } // 0-100
    public string StrengthLevel { get; set; } = "Weak"; // Weak, Fair, Good, Strong, Excellent
}

/// <summary>
/// Richiesta per il cambio password
/// </summary>
public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Richiesta per il reset password
/// </summary>
public class ResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Richiesta per il reset password con token
/// </summary>
public class ConfirmResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Richiesta per forzare il cambio password all'utente
/// </summary>
public class ForcePasswordChangeRequest
{
    public Guid UserId { get; set; }
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Informazioni sullo stato della password di un utente
/// </summary>
public class UserPasswordStatusDto
{
    public Guid UserId { get; set; }
    public DateTime? LastPasswordChangeDate { get; set; }
    public int DaysUntilExpiration { get; set; }
    public bool IsExpired { get; set; }
    public bool MustChangePassword { get; set; }
    public int FailedAccessAttempts { get; set; }
    public bool IsLockedOut { get; set; }
    public DateTime? LockoutEnd { get; set; }
}
