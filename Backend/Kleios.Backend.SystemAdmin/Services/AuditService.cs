using Kleios.Database.Context;
using Kleios.Database.Models;
using Kleios.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Kleios.Backend.SystemAdmin.Services;

public interface IAuditService
{
    Task<Option<IEnumerable<AuditLog>>> GetAuditLogsAsync(AuditLogFilter filter);
    Task<Option<AuditLog>> GetAuditLogByIdAsync(Guid id);
    Task<Option<IEnumerable<AuditLog>>> GetAuditLogsByResourceAsync(string resourceType, string resourceId);
    Task<Option<IEnumerable<AuditLog>>> GetAuditLogsByUserAsync(Guid userId);
    Task<Option> LogActionAsync(AuditLog auditLog);
    Task<Option> DeleteOldLogsAsync(DateTime olderThan);
}

/// <summary>
/// Servizio per la gestione dei log di audit
/// </summary>
public class AuditService : IAuditService
{
    private readonly KleiosDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(KleiosDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Option<IEnumerable<AuditLog>>> GetAuditLogsAsync(AuditLogFilter filter)
    {
        try
        {
            var query = _context.AuditLogs.AsQueryable();

            // Applica filtri
            if (filter.UserId.HasValue)
            {
                query = query.Where(a => a.UserId == filter.UserId.Value);
            }

            if (!string.IsNullOrEmpty(filter.Action))
            {
                query = query.Where(a => a.Action == filter.Action);
            }

            if (!string.IsNullOrEmpty(filter.ResourceType))
            {
                query = query.Where(a => a.ResourceType == filter.ResourceType);
            }

            if (!string.IsNullOrEmpty(filter.ResourceId))
            {
                query = query.Where(a => a.ResourceId == filter.ResourceId);
            }

            if (filter.StartDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(a => a.Timestamp <= filter.EndDate.Value);
            }

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(a => 
                    a.Description!.Contains(filter.SearchTerm) ||
                    a.Username!.Contains(filter.SearchTerm));
            }

            // Ordinamento
            query = query.OrderByDescending(a => a.Timestamp);

            // Paginazione
            var totalCount = await query.CountAsync();
            var logs = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return Option<IEnumerable<AuditLog>>.Success(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dei log di audit");
            return Option<IEnumerable<AuditLog>>.ServerError("Errore durante il recupero dei log");
        }
    }

    public async Task<Option<AuditLog>> GetAuditLogByIdAsync(Guid id)
    {
        try
        {
            var log = await _context.AuditLogs.FindAsync(id);
            if (log == null)
            {
                return Option<AuditLog>.NotFound("Log di audit non trovato");
            }

            return Option<AuditLog>.Success(log);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero del log di audit {Id}", id);
            return Option<AuditLog>.ServerError("Errore durante il recupero del log");
        }
    }

    public async Task<Option<IEnumerable<AuditLog>>> GetAuditLogsByResourceAsync(string resourceType, string resourceId)
    {
        try
        {
            var logs = await _context.AuditLogs
                .Where(a => a.ResourceType == resourceType && a.ResourceId == resourceId)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

            return Option<IEnumerable<AuditLog>>.Success(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dei log per risorsa {ResourceType}/{ResourceId}", resourceType, resourceId);
            return Option<IEnumerable<AuditLog>>.ServerError("Errore durante il recupero dei log");
        }
    }

    public async Task<Option<IEnumerable<AuditLog>>> GetAuditLogsByUserAsync(Guid userId)
    {
        try
        {
            var logs = await _context.AuditLogs
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

            return Option<IEnumerable<AuditLog>>.Success(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dei log per utente {UserId}", userId);
            return Option<IEnumerable<AuditLog>>.ServerError("Errore durante il recupero dei log");
        }
    }

    public async Task<Option> LogActionAsync(AuditLog auditLog)
    {
        try
        {
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Log di audit creato: {Action} su {ResourceType} da {Username}", 
                auditLog.Action, 
                auditLog.ResourceType, 
                auditLog.Username ?? "Sistema");

            return Option.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la creazione del log di audit");
            // Non propaghiamo l'errore per non bloccare l'operazione principale
            return Option.Success();
        }
    }

    public async Task<Option> DeleteOldLogsAsync(DateTime olderThan)
    {
        try
        {
            var logsToDelete = await _context.AuditLogs
                .Where(a => a.Timestamp < olderThan)
                .ToListAsync();

            _context.AuditLogs.RemoveRange(logsToDelete);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Eliminati {Count} log di audit più vecchi di {Date}", logsToDelete.Count, olderThan);

            return Option.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'eliminazione dei log vecchi");
            return Option.Failure("Errore durante l'eliminazione dei log");
        }
    }
}

/// <summary>
/// Filtro per la ricerca dei log di audit
/// </summary>
public class AuditLogFilter
{
    public Guid? UserId { get; set; }
    public string? Action { get; set; }
    public string? ResourceType { get; set; }
    public string? ResourceId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
