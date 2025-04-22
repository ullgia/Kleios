using Kleios.Backend.SystemAdmin.Services;
using Kleios.Shared.Authorization;
using Kleios.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kleios.Backend.SystemAdmin.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;
    
    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }
    
    [HttpGet]
    [Authorize(Policy = AppPermissions.Roles.View)]
    public async Task<IActionResult> GetAllRoles()
    {
        var result = await _roleService.GetAllRolesAsync();
        return result.IsSuccess 
            ? Ok(result.Value) 
            : StatusCode((int)result.StatusCode, result.Message);
    }
    
    [HttpGet("{id:guid}")]
    [Authorize(Policy = AppPermissions.Roles.View)]
    public async Task<IActionResult> GetRoleById(Guid id)
    {
        var result = await _roleService.GetRoleByIdAsync(id);
        return result.IsSuccess 
            ? Ok(result.Value) 
            : StatusCode((int)result.StatusCode, result.Message);
    }
    
    [HttpPost]
    [Authorize(Policy = AppPermissions.Roles.Manage)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        // Converti gli ID dei permessi nei loro nomi di sistema
        var permissionNames = await GetPermissionNamesFromIds(model.Permissions);
        
        var result = await _roleService.CreateRoleAsync(
            model.Name,
            model.Description ?? string.Empty,
            false, // I ruoli creati tramite API non sono di sistema per default
            permissionNames);
        
        return result.IsSuccess 
            ? CreatedAtAction(nameof(GetRoleById), new { id = result.Value.Id }, result.Value) 
            : StatusCode((int)result.StatusCode, result.Message);
    }
    
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AppPermissions.Roles.Manage)]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        // Converti gli ID dei permessi nei loro nomi di sistema se presenti
        IEnumerable<string>? permissionNames = null;
        if (model.Permissions != null)
        {
            permissionNames = await GetPermissionNamesFromIds(model.Permissions);
        }
        
        var result = await _roleService.UpdateRoleAsync(
            id,
            null, // Nome non modificabile
            model.Description,
            null, // IsSystemRole non modificabile tramite API
            permissionNames);
        
        return result.IsSuccess 
            ? Ok(result.Value) 
            : StatusCode((int)result.StatusCode, result.Message);
    }
    
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AppPermissions.Roles.Manage)]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        var result = await _roleService.DeleteRoleAsync(id);
        return result.IsSuccess 
            ? NoContent() 
            : StatusCode((int)result.StatusCode, result.Message);
    }
    
    [HttpGet("permissions")]
    [Authorize(Policy = AppPermissions.Roles.View)]
    public async Task<IActionResult> GetAllPermissions()
    {
        var result = await _roleService.GetAllPermissionsAsync();
        return result.IsSuccess 
            ? Ok(result.Value) 
            : StatusCode((int)result.StatusCode, result.Message);
    }
    
    // Metodo di supporto per convertire gli ID dei permessi nei loro nomi
    private async Task<IEnumerable<string>> GetPermissionNamesFromIds(IEnumerable<Guid> permissionIds)
    {
        var result = await _roleService.GetAllPermissionsAsync();
        if (!result.IsSuccess)
        {
            return Array.Empty<string>();
        }
        
        var permissions = result.Value;
        return permissionIds
            .Select(id => permissions.FirstOrDefault(p => p.Id == id)?.SystemName)
            .Where(name => name != null)
            .Cast<string>();
    }
}