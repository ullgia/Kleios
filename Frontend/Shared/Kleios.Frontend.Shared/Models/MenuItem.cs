namespace Kleios.Frontend.Shared.Models;

/// <summary>
/// Rappresenta un elemento del menu di navigazione dell'applicazione
/// </summary>
public class MenuItem
{
    public List<MenuItem> SubMenus { get; set; } = new();
    public required string Title { get; set; }
    public required string Icon { get; set; }
    public string? Href { get; set; }
    public string? Policy { get; set; }
    public bool IsDefaultOpen { get; set; }
}