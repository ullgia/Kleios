using Kleios.Backend.Shared;
using Kleios.Backend.SystemAdmin.Services;
using Kleios.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kleios.Backend.SystemAdmin.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    [Authorize(Policy = AppPermissions.Logs.View)]
    public async Task<Result> GetAuditLogs([FromQuery] AuditLogFilter filter)
    {
        var result = await _auditService.GetAuditLogsAsync(filter);
        return result;
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AppPermissions.Logs.View)]
    public async Task<Result> GetAuditLogById(Guid id)
    {
        var result = await _auditService.GetAuditLogByIdAsync(id);
        return result;
    }

    [HttpGet("resource/{resourceType}/{resourceId}")]
    [Authorize(Policy = AppPermissions.Logs.View)]
    public async Task<Result> GetAuditLogsByResource(string resourceType, string resourceId)
    {
        var result = await _auditService.GetAuditLogsByResourceAsync(resourceType, resourceId);
        return result;
    }

    [HttpGet("user/{userId:guid}")]
    [Authorize(Policy = AppPermissions.Logs.View)]
    public async Task<Result> GetAuditLogsByUser(Guid userId)
    {
        var result = await _auditService.GetAuditLogsByUserAsync(userId);
        return result;
    }

    [HttpDelete("cleanup")]
    [Authorize(Policy = AppPermissions.Logs.Manage)]
    public async Task<Result> DeleteOldLogs([FromQuery] DateTime olderThan)
    {
        var result = await _auditService.DeleteOldLogsAsync(olderThan);
        return result;
    }
}
