using Kleios.Backend.SystemAdmin.Services;
using Kleios.Shared.Authorization;
using Kleios.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kleios.Backend.SystemAdmin.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SessionController : ControllerBase
{
    private readonly ISessionManagementService _sessionService;
    private readonly ILogger<SessionController> _logger;

    public SessionController(
        ISessionManagementService sessionService,
        ILogger<SessionController> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>
    /// Ottiene le sessioni attive per l'utente corrente
    /// </summary>
    [HttpGet("my-sessions")]
    public async Task<ActionResult<IEnumerable<UserSessionDto>>> GetMySessions()
    {
        try
        {
            var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Utente non autenticato");
            }

            var sessions = await _sessionService.GetUserSessionsAsync(userId);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero delle sessioni");
            return StatusCode(500, "Errore interno del server");
        }
    }

    /// <summary>
    /// Ottiene le sessioni attive per un utente specifico (admin only)
    /// </summary>
    [HttpGet("user/{userId}")]
    [Authorize(Policy = AppPermissions.Users.View)]
    public async Task<ActionResult<IEnumerable<UserSessionDto>>> GetUserSessions(Guid userId)
    {
        try
        {
            var sessions = await _sessionService.GetUserSessionsAsync(userId);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero delle sessioni per l'utente {UserId}", userId);
            return StatusCode(500, "Errore interno del server");
        }
    }

    /// <summary>
    /// Termina una sessione specifica
    /// </summary>
    [HttpDelete("{sessionId}")]
    public async Task<ActionResult> TerminateSession(Guid sessionId)
    {
        try
        {
            var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Utente non autenticato");
            }

            var success = await _sessionService.TerminateSessionAsync(sessionId, userId);
            if (success)
            {
                return Ok(new { message = "Sessione terminata con successo" });
            }

            return BadRequest("Impossibile terminare la sessione");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la terminazione della sessione {SessionId}", sessionId);
            return StatusCode(500, "Errore interno del server");
        }
    }

    /// <summary>
    /// Termina tutte le sessioni dell'utente corrente (eccetto quella corrente)
    /// </summary>
    [HttpDelete("my-sessions/terminate-all")]
    public async Task<ActionResult> TerminateAllMySessions()
    {
        try
        {
            var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Utente non autenticato");
            }

            // Ottieni l'ID della sessione corrente dal token JWT (claim "jti")
            var currentJwtId = User.FindFirst("jti")?.Value;
            Guid? currentSessionJwtId = null;
            
            if (!string.IsNullOrEmpty(currentJwtId) && Guid.TryParse(currentJwtId, out var jwtGuid))
            {
                currentSessionJwtId = jwtGuid;
                _logger.LogDebug("Sessione corrente identificata: JwtId={JwtId}", currentJwtId);
            }
            
            var success = await _sessionService.TerminateAllSessionsAsync(userId, currentSessionJwtId);
            
            if (success)
            {
                return Ok(new { message = "Tutte le altre sessioni sono state terminate" });
            }

            return BadRequest("Impossibile terminare le sessioni");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la terminazione delle sessioni");
            return StatusCode(500, "Errore interno del server");
        }
    }

    /// <summary>
    /// Ottiene le statistiche delle sessioni per l'utente corrente
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<SessionStatisticsDto>> GetMySessionStatistics()
    {
        try
        {
            var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Utente non autenticato");
            }

            var statistics = await _sessionService.GetSessionStatisticsAsync(userId);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero delle statistiche");
            return StatusCode(500, "Errore interno del server");
        }
    }

    /// <summary>
    /// Ottiene la configurazione delle sessioni
    /// </summary>
    [HttpGet("configuration")]
    [Authorize(Policy = AppPermissions.Settings.View)]
    public async Task<ActionResult<SessionConfigurationDto>> GetConfiguration()
    {
        try
        {
            var config = await _sessionService.GetSessionConfigurationAsync();
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero della configurazione");
            return StatusCode(500, "Errore interno del server");
        }
    }

    /// <summary>
    /// Aggiorna la configurazione delle sessioni
    /// </summary>
    [HttpPut("configuration")]
    [Authorize(Policy = AppPermissions.Settings.Manage)]
    public async Task<ActionResult<SessionConfigurationDto>> UpdateConfiguration([FromBody] SessionConfigurationDto config)
    {
        try
        {
            var updatedConfig = await _sessionService.UpdateSessionConfigurationAsync(config);
            return Ok(updatedConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'aggiornamento della configurazione");
            return StatusCode(500, "Errore interno del server");
        }
    }
}
