using Kleios.Backend.Shared;
using Kleios.Backend.SystemAdmin.Services;
using Kleios.Shared.Authorization;
using Kleios.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kleios.Backend.SystemAdmin.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;
    
    public SettingsController(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }
    
    [HttpGet]
    [Authorize(Policy = AppPermissions.Settings.View)]
    public async Task<IActionResult> GetAllSettings([FromQuery] SettingFilter? filter)
    {
        var result = await _settingsService.GetAllSettingsAsync();
        return result.IsSuccess 
            ? Ok(result.Value) 
            : StatusCode((int)result.StatusCode, result.Message);
    }
    
    [HttpGet("category/{category}")]
    [Authorize(Policy = AppPermissions.Settings.View)]
    public async Task<IActionResult> GetSettingsByCategory(string category)
    {
        var result = await _settingsService.GetSettingsByCategoryAsync(category);
        return result.IsSuccess 
            ? Ok(result.Value) 
            : StatusCode((int)result.StatusCode, result.Message);
    }
    
    [HttpGet("{key}")]
    [Authorize(Policy = AppPermissions.Settings.View)]
    public async Task<IActionResult> GetSettingByKey(string key)
    {
        var result = await _settingsService.GetSettingByKeyAsync(key);
        return result.IsSuccess 
            ? Ok(result.Value) 
            : StatusCode((int)result.StatusCode, result.Message);
    }
    
    [HttpPut("{key}")]
    [Authorize(Policy = AppPermissions.Settings.Manage)]
    public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSettingRequest model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var result = await _settingsService.UpdateSettingAsync(key, model.Value);
        return result.IsSuccess 
            ? Ok(result.Value) 
            : StatusCode((int)result.StatusCode, result.Message);
    }
    
    [HttpPost]
    [Authorize(Policy = AppPermissions.Settings.Manage)]
    public async Task<IActionResult> CreateSetting([FromBody] CreateSettingRequest model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var result = await _settingsService.CreateSettingAsync(
            model.Key,
            model.Value,
            model.Description,
            model.DataType,
            model.IsRequired,
            model.IsReadOnly,
            model.Category);
        
        return result.IsSuccess 
            ? CreatedAtAction(nameof(GetSettingByKey), new { key = model.Key }, result.Value) 
            : StatusCode((int)result.StatusCode, result.Message);
    }
    
    [HttpDelete("{key}")]
    [Authorize(Policy = AppPermissions.Settings.Manage)]
    public async Task<IActionResult> DeleteSetting(string key)
    {
        var result = await _settingsService.DeleteSettingAsync(key);
        return result.IsSuccess 
            ? NoContent() 
            : StatusCode((int)result.StatusCode, result.Message);
    }
}