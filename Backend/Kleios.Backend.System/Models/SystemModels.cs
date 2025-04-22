namespace Kleios.Backend.SystemAdmin.Models;

/// <summary>
/// Model for user creation/update requests
/// </summary>
public class UserModel
{
    public Guid? Id { get; set; }
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public string? Password { get; set; }
    public List<string> Roles { get; set; } = new();
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Model for role creation/update requests
/// </summary>
public class RoleModel
{
    public Guid? Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
    public bool IsSystemRole { get; set; } = false;
}

/// <summary>
/// Model for permission creation/update requests
/// </summary>
public class PermissionModel
{
    public Guid? Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "System";
}

/// <summary>
/// Model for system setting categories
/// </summary>
public class SettingCategoryModel
{
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<SettingModel> Settings { get; set; } = new();
}

/// <summary>
/// Model for system settings
/// </summary>
public class SettingModel
{
    public required string Key { get; set; }
    public string? Value { get; set; }
    public string Description { get; set; } = string.Empty;
    public string DataType { get; set; } = "string";
    public bool IsRequired { get; set; } = false;
    public bool IsReadOnly { get; set; } = false;
    public string Category { get; set; } = "General";
}