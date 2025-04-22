namespace Kleios.Shared.Models;

/// <summary>
/// DTO per la creazione di un'impostazione di sistema
/// </summary>
public class CreateSettingRequest
{
    public required string Key { get; set; }
    public string? Value { get; set; }
    public string Description { get; set; } = string.Empty;
    public string DataType { get; set; } = "string";
    public bool IsRequired { get; set; } = false;
    public bool IsReadOnly { get; set; } = false;
    public string Category { get; set; } = "General";
}

/// <summary>
/// DTO per l'aggiornamento di un'impostazione di sistema
/// </summary>
public class UpdateSettingRequest
{
    public string? Value { get; set; }
    public string? Description { get; set; }
    public bool? IsRequired { get; set; }
    public bool? IsReadOnly { get; set; }
}

/// <summary>
/// Filtro per le impostazioni di sistema
/// </summary>
public class SettingFilter
{
    public string? Category { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// DTO per un'impostazione di sistema
/// </summary>
public class SettingDto
{
    public Guid Id { get; set; }
    public required string Key { get; set; }
    public string? Value { get; set; }
    public string Description { get; set; } = string.Empty;
    public string DataType { get; set; } = "string";
    public bool IsRequired { get; set; }
    public bool IsReadOnly { get; set; }
    public string Category { get; set; } = "General";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO per una categoria di impostazioni
/// </summary>
public class SettingCategoryDto
{
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<SettingDto> Settings { get; set; } = new List<SettingDto>();
}