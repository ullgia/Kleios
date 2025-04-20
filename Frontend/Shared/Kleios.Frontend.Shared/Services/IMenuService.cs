using System.Collections.Generic;
using System.Threading.Tasks;
using Kleios.Frontend.Shared.Models;

namespace Kleios.Frontend.Shared.Services;

/// <summary>
/// Interfaccia per il servizio di gestione del menu di navigazione
/// </summary>
public interface IMenuService
{
    /// <summary>
    /// Recupera gli elementi del menu in base ai permessi dell'utente corrente
    /// </summary>
    Task<List<MenuItem>> GetMenuItemsAsync();
}