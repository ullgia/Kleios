using Kleios.Backend.SystemAdmin.Services;
using Kleios.Shared.Authorization;
using Kleios.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kleios.Backend.SystemAdmin.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PasswordPolicyController : ControllerBase
{
    private readonly IPasswordPolicyService _passwordPolicyService;
    private readonly ILogger<PasswordPolicyController> _logger;

    public PasswordPolicyController(
        IPasswordPolicyService passwordPolicyService,
        ILogger<PasswordPolicyController> logger)
    {
        _passwordPolicyService = passwordPolicyService;
        _logger = logger;
    }

    /// <summary>
    /// Ottiene la password policy corrente
    /// </summary>
    [HttpGet("policy")]
    [Authorize(Policy = AppPermissions.Settings.View)]
    public async Task<ActionResult<PasswordPolicyDto>> GetPasswordPolicy()
    {
        try
        {
            var policy = await _passwordPolicyService.GetPasswordPolicyAsync();
            return Ok(policy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero della password policy");
            return StatusCode(500, "Errore interno del server");
        }
    }

    /// <summary>
    /// Aggiorna la password policy
    /// </summary>
    [HttpPut("policy")]
    [Authorize(Policy = AppPermissions.Settings.Manage)]
    public async Task<ActionResult<PasswordPolicyDto>> UpdatePasswordPolicy([FromBody] PasswordPolicyDto policy)
    {
        try
        {
            var updatedPolicy = await _passwordPolicyService.UpdatePasswordPolicyAsync(policy);
            return Ok(updatedPolicy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'aggiornamento della password policy");
            return StatusCode(500, "Errore interno del server");
        }
    }

    /// <summary>
    /// Valida una password in base alla policy corrente
    /// </summary>
    [HttpPost("validate")]
    [AllowAnonymous]
    public async Task<ActionResult<PasswordValidationResult>> ValidatePassword([FromBody] string password)
    {
        try
        {
            var result = await _passwordPolicyService.ValidatePasswordAsync(password);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la validazione della password");
            return StatusCode(500, "Errore interno del server");
        }
    }

    /// <summary>
    /// Cambia la password dell'utente corrente
    /// </summary>
    [HttpPost("change")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Utente non autenticato");
            }

            var success = await _passwordPolicyService.ChangePasswordAsync(userId, request);
            if (success)
            {
                return Ok(new { message = "Password cambiata con successo" });
            }

            return BadRequest("Impossibile cambiare la password. Verifica che la password corrente sia corretta e che la nuova password rispetti i requisiti.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il cambio password");
            return StatusCode(500, "Errore interno del server");
        }
    }

    /// <summary>
    /// Forza il cambio password per un utente (solo admin)
    /// </summary>
    [HttpPost("force-change/{userId}")]
    [Authorize(Policy = AppPermissions.Users.Manage)]
    public async Task<ActionResult> ForceChangePassword(Guid userId, [FromBody] string newPassword)
    {
        try
        {
            var success = await _passwordPolicyService.ForceChangePasswordAsync(userId, newPassword);
            if (success)
            {
                return Ok(new { message = "Password resettata con successo" });
            }

            return BadRequest("Impossibile resettare la password");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il reset password per l'utente {UserId}", userId);
            return StatusCode(500, "Errore interno del server");
        }
    }

    /// <summary>
    /// Invia email per il reset della password
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult> ForgotPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            await _passwordPolicyService.SendPasswordResetEmailAsync(request.Email);
            return Ok(new { message = "Se l'email esiste nel sistema, riceverai le istruzioni per il reset della password" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la richiesta di reset password");
            return StatusCode(500, "Errore interno del server");
        }
    }

    /// <summary>
    /// Conferma il reset della password con il token
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult> ResetPassword([FromBody] ConfirmResetPasswordRequest request)
    {
        try
        {
            var success = await _passwordPolicyService.ResetPasswordAsync(request);
            if (success)
            {
                return Ok(new { message = "Password resettata con successo" });
            }

            return BadRequest("Token non valido o scaduto");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il reset password");
            return StatusCode(500, "Errore interno del server");
        }
    }

    /// <summary>
    /// Ottiene lo stato della password per un utente
    /// </summary>
    [HttpGet("status/{userId}")]
    [Authorize(Policy = AppPermissions.Users.View)]
    public async Task<ActionResult<UserPasswordStatusDto>> GetPasswordStatus(Guid userId)
    {
        try
        {
            var status = await _passwordPolicyService.GetUserPasswordStatusAsync(userId);
            return Ok(status);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dello stato della password per l'utente {UserId}", userId);
            return StatusCode(500, "Errore interno del server");
        }
    }

    /// <summary>
    /// Ottiene gli utenti con password scadute
    /// </summary>
    [HttpGet("expired")]
    [Authorize(Policy = AppPermissions.Users.View)]
    public async Task<ActionResult<IEnumerable<UserPasswordStatusDto>>> GetExpiredPasswords()
    {
        try
        {
            var expiredPasswords = await _passwordPolicyService.GetExpiredPasswordsAsync();
            return Ok(expiredPasswords);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero delle password scadute");
            return StatusCode(500, "Errore interno del server");
        }
    }
}
