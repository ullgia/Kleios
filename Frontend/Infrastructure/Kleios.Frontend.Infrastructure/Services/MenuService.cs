using System.Security.Claims;
using Kleios.Frontend.Shared.Models;
using Kleios.Shared.Authorization;
using Microsoft.AspNetCore.Components.Authorization;

namespace Kleios.Frontend.Infrastructure.Services;

/// <summary>
/// Implementazione del servizio di gestione del menu di navigazione
/// </summary>
public class MenuService : Kleios.Frontend.Shared.Services.IMenuService
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly List<MenuItem> _menuItems;

    public MenuService(AuthenticationStateProvider authStateProvider)
    {
        _authStateProvider = authStateProvider;
        
        // Definizione statica degli elementi del menu utilizzando le policy da AppPermissions
        _menuItems = new List<MenuItem>
        {
            new MenuItem
            {
                Title = "Home",
                Icon = "fas fa-home",
                Href = "/"
            },
            new MenuItem
            {
                Title = "Amministrazione",
                Icon = "fas fa-cogs",
                IsDefaultOpen = false,
                SubMenus = new List<MenuItem>
                {
                    new MenuItem
                    {
                        Title = "Gestione Utenti",
                        Icon = "fas fa-users",
                        Href = "/System/Users",
                        Policy = AppPermissions.Users.View
                    },
                    new MenuItem
                    {
                        Title = "Impostazioni",
                        Icon = "fas fa-cog",
                        Href = "/System/Settings",
                        Policy = AppPermissions.Settings.View
                    },
                    new MenuItem
                    {
                        Title = "Logs di Sistema",
                        Icon = "fas fa-list",
                        Href = "/System/Logs",
                        Policy = AppPermissions.Logs.View
                    }
                }
            }
        };
    }

    /// <summary>
    /// Recupera gli elementi del menu in base ai permessi dell'utente corrente
    /// </summary>
    public async Task<List<MenuItem>> GetMenuItemsAsync()
    {
        var authenticationState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authenticationState.User;
        var userClaims = user.Claims.ToList();
        return CheckMenus(_menuItems, userClaims);
    }

    /// <summary>
    /// Filtra gli elementi del menu in base ai permessi dell'utente
    /// </summary>
    private static List<MenuItem> CheckMenus(List<MenuItem> menuItems, List<Claim> userClaims)
    {
        var result = new List<MenuItem>();
        foreach (var menuItem in menuItems)
        {
            if (menuItem.Href == null)
            {
                var subMenus = CheckMenus(menuItem.SubMenus, userClaims);
                if (subMenus.Count > 0)
                {
                    result.Add(new MenuItem
                    {
                        Title = menuItem.Title,
                        Icon = menuItem.Icon,
                        Href = menuItem.Href,
                        SubMenus = subMenus,
                        IsDefaultOpen = menuItem.IsDefaultOpen
                    });
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(menuItem.Policy) || userClaims.Any(x =>
                        x.Type == ApplicationClaimTypes.Permission && x.Value == menuItem.Policy))
                {
                    result.Add(new MenuItem
                    {
                        Title = menuItem.Title,
                        Icon = menuItem.Icon,
                        Href = menuItem.Href
                    });
                }
            }
        }

        return result;
    }
}