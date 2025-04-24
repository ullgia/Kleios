using Kleios.Backend.SystemAdmin.Services;
using Kleios.Shared.Authorization;
using Kleios.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kleios.Backend.SystemAdmin.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    
    public UsersController(IUserService userService)
    {
        _userService = userService;
    }
    
    [HttpGet]
    //[Authorize(Policy = AppPermissions.Users.View)]
    public async Task<IActionResult> GetAllUsers([FromQuery] UserFilter? filter)
    {
        var result = await _userService.GetAllUsersAsync();
        return result.IsSuccess 
            ? Ok(result.Value) 
            : StatusCode((int)result.StatusCode, result.Message);
    }
    
    [HttpGet("{id:guid}")]
    [Authorize(Policy = AppPermissions.Users.View)]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var result = await _userService.GetUserByIdAsync(id);
        return result.IsSuccess 
            ? Ok(result.Value) 
            : StatusCode((int)result.StatusCode, result.Message);
    }
    
    [HttpPost]
    [Authorize(Policy = AppPermissions.Users.Manage)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var result = await _userService.CreateUserAsync(
            model.Username, 
            model.Email, 
            model.Password,
            model.Roles);
        
        return result.IsSuccess 
            ? CreatedAtAction(nameof(GetUserById), new { id = result.Value.Id }, result.Value) 
            : StatusCode((int)result.StatusCode, result.Message);
    }
    
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AppPermissions.Users.Manage)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var result = await _userService.UpdateUserAsync(
            id,
            null, // Username non modificabile
            model.Email,
            model.Password,
            model.Roles);
        
        return result.IsSuccess 
            ? Ok(result.Value) 
            : StatusCode((int)result.StatusCode, result.Message);
    }
    
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AppPermissions.Users.Manage)]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var result = await _userService.DeleteUserAsync(id);
        return result.IsSuccess 
            ? NoContent() 
            : StatusCode((int)result.StatusCode, result.Message);
    }
}