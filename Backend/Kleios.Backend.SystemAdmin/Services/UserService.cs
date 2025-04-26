using Kleios.Database.Context;
using Kleios.Database.Models;
using Kleios.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kleios.Backend.SystemAdmin.Services;

public interface IUserService
{
    Task<Option<IEnumerable<ApplicationUser>>> GetAllUsersAsync();
    Task<Option<ApplicationUser>> GetUserByIdAsync(Guid id);
    Task<Option<ApplicationUser>> CreateUserAsync(string username, string email, string password, IEnumerable<string> roles);
    Task<Option<ApplicationUser>> UpdateUserAsync(Guid id, string? username, string? email, string? password, IEnumerable<string>? roles);
    Task<Option> DeleteUserAsync(Guid id);
}

/// <summary>
/// Service for user management operations
/// </summary>
public class UserService : IUserService
{
    private readonly KleiosDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    
    public UserService(
        KleiosDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }
    
    public async Task<Option<IEnumerable<ApplicationUser>>> GetAllUsersAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        return Option<IEnumerable<ApplicationUser>>.Success(users);
    }
    
    public async Task<Option<ApplicationUser>> GetUserByIdAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return Option<ApplicationUser>.NotFound("User not found");
        }
        
        return Option<ApplicationUser>.Success(user);
    }
    
    public async Task<Option<ApplicationUser>> CreateUserAsync(string username, string email, string password, IEnumerable<string> roles)
    {
        var user = new ApplicationUser
        {
            UserName = username,
            Email = email
        };
        
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return Option<ApplicationUser>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
        
        if (roles.Any())
        {
            foreach (var roleName in roles)
            {
                var roleExists = await _roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                {
                    return Option<ApplicationUser>.Failure($"Role '{roleName}' does not exist");
                }
                
                await _userManager.AddToRoleAsync(user, roleName);
            }
        }
        
        return Option<ApplicationUser>.Success(user);
    }
    
    public async Task<Option<ApplicationUser>> UpdateUserAsync(Guid id, string? username, string? email, string? password, IEnumerable<string>? roles)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return Option<ApplicationUser>.NotFound("User not found");
        }
        
        if (!string.IsNullOrEmpty(username))
        {
            user.UserName = username;
        }
        
        if (!string.IsNullOrEmpty(email))
        {
            user.Email = email;
        }
        
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return Option<ApplicationUser>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
        
        if (!string.IsNullOrEmpty(password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, password);
            if (!resetResult.Succeeded)
            {
                return Option<ApplicationUser>.Failure(string.Join(", ", resetResult.Errors.Select(e => e.Description)));
            }
        }
        
        if (roles != null && roles.Any())
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            var removeResult = await _userManager.RemoveFromRolesAsync(user, userRoles);
            if (!removeResult.Succeeded)
            {
                return Option<ApplicationUser>.Failure(string.Join(", ", removeResult.Errors.Select(e => e.Description)));
            }
            
            foreach (var roleName in roles)
            {
                var roleExists = await _roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                {
                    return Option<ApplicationUser>.Failure($"Role '{roleName}' does not exist");
                }
                
                await _userManager.AddToRoleAsync(user, roleName);
            }
        }
        
        return Option<ApplicationUser>.Success(user);
    }
    
    public async Task<Option> DeleteUserAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return Option.NotFound("User not found");
        }
        
        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return Option.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
        
        return Option.Success();
    }
}