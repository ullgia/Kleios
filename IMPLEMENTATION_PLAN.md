# Piano di Implementazione - Kleios Frontend Architecture
## Gateway con Service Discovery e Moduli Autonomi

**Data Inizio**: 10 Ottobre 2025  
**Architettura**: Gateway-based routing con prefix matching + WebSocket heartbeat  
**Stack**: Blazor SSR, .NET 9, MudBlazor, YARP, Aspire

---

## 🎯 Obiettivi

- [x] Definire architettura modulare con Gateway centrale
- [ ] Implementare Gateway con service registry in-memory
- [ ] Creare 3 moduli indipendenti (Auth, Home, System)
- [ ] Configurare routing basato su prefix (/auth, /system, /)
- [ ] Implementare authentication condivisa (JWT + Cookie)
- [ ] Setup layout e navigazione condivisi
- [ ] Integrare con Aspire per service discovery

---

## ⚠️ Punti Critici da Non Dimenticare

### 🔐 Cookie Configuration
- **Domain**: DEVE essere `null` in development, altrimenti cookie non funziona su localhost
- **SameSite**: DEVE essere `Lax` (non `Strict`) per permettere redirect tra moduli
- **Path**: `/` per condividere tra tutti i moduli
- **HttpOnly**: `true` per sicurezza
- **Secure**: `true` solo in HTTPS (development usa certificati dev)

### 🌐 CORS Configuration (Backend)
- Backend DEVE permettere requests da Gateway URL
- Backend DEVE permettere credentials: `AllowCredentials = true`
- Backend DEVE specificare origins esatti (no wildcard con credentials)

### 📍 Base Href e Static Assets
- Ogni modulo ha `base href` diverso (`/auth/`, `/system/`, `/`)
- MudBlazor assets serviti dal Gateway: `{GATEWAY_URL}/_content/MudBlazor/`
- CSS locale del modulo: `/app.css` (relativo al base href)
- **ATTENZIONE**: Se base href è `/auth/`, allora `/app.css` diventa `/auth/app.css`

### 🔄 Path Rewriting nei Moduli
Ogni modulo (tranne Home) DEVE avere middleware per path rewriting:
```csharp
// Auth Module: toglie /auth dal path prima di processare
app.Use((context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/auth", out var remainder))
    {
        context.Request.Path = remainder;
        context.Request.PathBase = "/auth";
    }
    return next();
});
```

### 🔌 WebSocket e Health Check
- Health check DEVE rispondere entro 5s (timeout Gateway)
- WebSocket heartbeat ogni 30s
- Moduli DEVONO riconnettersi automaticamente dopo disconnect
- Gateway DEVE deregistrare dopo 3 failed health checks (90s)

### 🎯 Route Prefix Matching
- **ORDINE IMPORTANTE**: Longest match first!
  - `/system/Users` deve matchare PRIMA di `/`
  - Gateway ordina prefixes per lunghezza decrescente
- **Trailing slash**: `/auth` matcha sia `/auth/Login` che `/auth`

### 🔑 JWT e Claims
- JWT DEVE contenere almeno: `sub` (user id), `email`, `roles`
- Token expiration: 60 minuti (allineato con cookie sliding)
- Refresh token: gestito dal Backend (future)
- Claims parsing: usare `JwtSecurityTokenHandler`

---

## 📋 Fasi di Implementazione

### FASE 1: Librerie Condivise e Contratti

#### 1.1 Kleios.Frontend.Shared (Libreria di Contratti) ✅
- [x] Creare progetto Razor Class Library
  - [x] `dotnet new classlib -n Kleios.Frontend.Shared -f net9.0`
- [x] Implementare `ServiceRegistration.cs`
  ```csharp
  - ServiceName (string)
  - RoutePrefix (string)  
  - BaseUrl (string)
  - HealthCheckEndpoint (string)
  ```
- [x] Implementare `GatewayConnectionClient.cs`
  - [x] Metodo `RegisterAsync()` - HTTP POST a Gateway
  - [x] Metodo `ConnectWebSocketAsync()` - Connessione persistente
  - [x] Metodo `SendHeartbeatAsync()` - Keep-alive ogni 30s
  - [x] Auto-retry logic con exponential backoff
- [x] Creare `KleiosConstants.cs`
  - [x] Cookie name: `Kleios.AuthToken`
  - [x] Gateway registration endpoint: `/api/_gateway/register`
  - [x] Health check path: `/_health`
  - [x] Auth paths: `/auth/Account/Login`, `/auth/Account/Logout`
- [x] Aggiungere package references
  - [x] `Microsoft.Extensions.Logging.Abstractions`
  - [x] `Microsoft.Extensions.Http`

#### 1.2 Kleios.Frontend.Infrastructure (Authentication e Services) ✅
- [x] Creare progetto Class Library
  - [x] `dotnet new classlib -n Kleios.Frontend.Infrastructure -f net9.0`
- [x] Implementare `Authentication/CookieAuthenticationExtensions.cs`
  - [x] Extension method `AddKleiosCookieAuthentication()`
  - [x] Configurazione unificata cookie (name, domain, expiration, paths)
  - [x] Supporto per Development (domain: null) e Production (domain: configurabile)
  - [x] LoginPath, LogoutPath, AccessDeniedPath da KleiosConstants
- [x] Implementare `Authentication/ServerCookieAuthenticationStateProvider.cs`
  - [x] Custom AuthenticationStateProvider
  - [x] Legge cookie HttpContext
  - [x] Parsing JWT claims per creare ClaimsPrincipal
  - [ ] Cache dei claims con FusionCache (optional) - FUTURE
- [x] Implementare `Services/IAuthenticationService.cs`
  - [x] `Task<Option<LoginResponse>> LoginAsync(LoginRequest)`
  - [x] `Task<Option> LogoutAsync()`
  - [x] `Task<Option<UserInfo>> GetCurrentUserAsync()`
  - [x] `Task<Option<bool>> ValidateTokenAsync()`
- [x] Implementare `Services/AuthenticationService.cs`
  - [x] HttpClient per chiamare Backend Auth API
  - [x] JWT parsing e validazione
  - [x] Cookie management (set/clear)
  - [x] Error handling e retry logic
- [ ] Implementare `Authorization/KleiosPolicyProvider.cs` - FUTURE (non essenziale per MVP)
  - [ ] Custom IAuthorizationPolicyProvider
  - [ ] Politiche dinamiche basate su permessi
  - [ ] Cache policies per performance
- [x] Implementare `Services/ServiceCollectionExtensions.cs`
  - [x] Extension method `AddKleiosInfrastructure()`
  - [x] Registra tutti i servizi (Auth, HttpClient, etc.)
  - [ ] Configura authorization policies - FUTURE
- [x] Aggiungere package references
  - [x] `Microsoft.AspNetCore.Authentication.Cookies`
  - [x] `Microsoft.AspNetCore.Components.Authorization`
  - [x] `System.IdentityModel.Tokens.Jwt`
  - [ ] `ZiggyCreatures.FusionCache` (optional per cache) - FUTURE

#### 1.3 Kleios.Frontend.Components (Layout e UI Condivisi) ✅
- [x] Creare progetto Razor Class Library
  - [x] `dotnet new razorclasslib -n Kleios.Frontend.Components -f net9.0`
- [x] Implementare `Layout/MainLayout.razor`
  - [x] MudLayout con drawer per menu
  - [x] Header con logo e user info
  - [x] Menu utente con Profilo e Logout
  - [x] Integrazione MudBlazor
- [x] Implementare `Layout/NavMenu.razor`
  - [x] Caricamento dinamico menu dal Gateway (con mock data per MVP)
  - [x] API call preparata: `GET /api/_gateway/routes`
  - [x] Rendering MudNavMenu con routes
- [x] Implementare `App/AuthorizedRouteView.razor`
  - [x] Router con AuthorizeRouteView
  - [x] Stati: Authorizing, NotAuthorized, Authorized
  - [x] Loading spinner con MudProgressCircular
  - [x] NotFound con pagina 404 personalizzata
- [x] Implementare `App/RedirectToLogin.razor`
  - [x] NavigationManager.NavigateTo con returnUrl
  - [x] Force reload per cambio modulo
- [x] Implementare `App/RedirectToAccessDenied.razor`
  - [x] Pagina 403 con MudBlazor
  - [x] Pulsanti per tornare alla Home o Indietro
- [x] Aggiungere package references
  - [x] `MudBlazor` (8.13.0)
  - [x] `Microsoft.AspNetCore.Components.Authorization` (9.0.9)
  - [x] `Microsoft.Extensions.Logging.Abstractions` (9.0.9)
  - [x] `Microsoft.Extensions.Http` (9.0.9)
- [x] Aggiungere project reference
  - [x] `Kleios.Frontend.Shared`

---

### FASE 2: Gateway (Core dell'Architettura) ✅

#### 2.1 Kleios.Gateway - Progetto Base ✅
- [x] Creare progetto ASP.NET Core Empty
  - [x] `dotnet new web -n Kleios.Gateway -f net9.0`
- [x] Aggiungere package references
  - [x] `Yarp.ReverseProxy` (2.3.0)
  - [x] `MudBlazor` (8.13.0 - per servire static assets)
- [x] Aggiungere project reference
  - [x] `Kleios.Frontend.Shared`

#### 2.2 Service Registry (In-Memory) ✅
- [x] Creare `Services/IServiceRegistry.cs` (Interface)
  - [x] `Task RegisterServiceAsync(ServiceRegistration)`
  - [x] `Task<ServiceRegistration?> GetServiceByPrefixAsync(string prefix)`
  - [x] `Task<IEnumerable<ServiceRegistration>> GetAllServicesAsync()`
  - [x] `Task UnregisterServiceAsync(string serviceName)`
  - [x] `Task UpdateHealthStatusAsync(string serviceName, bool isHealthy)`
  - [x] `Task<int> GetServiceCountAsync()`
- [x] Implementare `Services/InMemoryServiceRegistry.cs`
  - [x] ConcurrentDictionary per thread-safety
  - [x] Prefix matching con longest-match-first (ordinamento)
  - [x] **IsValidPrefixMatch** - Validazione corretta del prefix
    - [x] Path deve iniziare con prefix
    - [x] Se path più lungo, dopo prefix DEVE esserci `/`
    - [x] Previene match errati: `/authentication` non matcha `/auth`
    - [x] Esempi: `/auth/login` ✅ matcha `/auth`, `/authentication` ❌ non matcha `/auth`
  - [x] Health status tracking
  - [x] Logging dettagliato per registrazioni e aggiornamenti
- [x] Implementare `Services/ServiceHealthMonitor.cs` (Background Service)
  - [x] Timer ogni 30s per controllare health (configurabile da KleiosConstants)
  - [x] HTTP GET a `{serviceUrl}/_health` con timeout 5s
  - [x] Auto-deregister dopo 3 failed checks consecutivi
  - [x] Logging degli eventi

#### 2.3 WebSocket Manager ✅
- [x] Creare `Services/IWebSocketManager.cs`
  - [x] `Task HandleWebSocketAsync(WebSocket, string serviceName)`
  - [x] `Task SendToServiceAsync(string serviceName, string message)`
  - [x] `Task BroadcastAsync(string message)`
  - [x] `int GetActiveConnectionsCount()`
- [x] Implementare `Services/WebSocketManager.cs`
  - [x] ConcurrentDictionary<serviceName, WebSocket>
  - [x] Heartbeat handler (riceve ping, risponde pong)
  - [x] Connection close handler → auto-deregister
  - [x] Gestione errori e disconnessioni

#### 2.4 Controllers e Endpoints ✅
- [x] Creare `Controllers/GatewayController.cs`
  - [x] `POST /api/_gateway/register` - Registrazione servizio
    - [x] Validazione ServiceRegistration completa
    - [x] Chiamata a ServiceRegistry.RegisterServiceAsync()
    - [x] Risposta 200 OK o 400 BadRequest
  - [x] `GET /api/_gateway/routes` - Lista route disponibili
    - [x] Query a ServiceRegistry.GetAllServicesAsync()
    - [x] Filtro solo servizi healthy
    - [x] Risposta JSON con route list
  - [x] `GET /api/_gateway/services` - Stato servizi (admin)
    - [x] Health status di tutti i servizi
    - [x] Timestamp ultima registrazione
    - [x] Statistiche aggregate
  - [x] `DELETE /api/_gateway/register/{serviceName}` - Deregistrazione manuale
- [x] Creare WebSocket Endpoint in Program.cs
  - [x] Map endpoint `/ws/{serviceName}`
  - [x] Upgrade HTTP a WebSocket
  - [x] Chiamata a WebSocketManager.HandleWebSocketAsync()

#### 2.5 Middleware e YARP Configuration ✅
- [x] Creare `Middleware/PrefixRoutingMiddleware.cs`
  - [x] Intercetta request in arrivo
  - [x] Ignora API Gateway (`/api/_gateway`)
  - [x] Ignora WebSocket endpoints (`/ws/`)
  - [x] Ignora static assets (`/_content/`, `.css`, `.js`)
  - [x] Query a ServiceRegistry per trovare target service
  - [x] Forward a YARP usando IHttpForwarder
  - [x] Gestione errori 404 e 502
  - [x] **IMPORTANTE**: Path preservato (modulo gestisce prefix)
- [x] Integrare YARP in Program.cs
  - [x] AddHttpForwarder() per forwarding dinamico
  - [x] IHttpForwarder usato nel middleware custom
  - [x] Headers forwarding automatico (cookie, authorization)
- [ ] Implementare `Services/YarpConfigProvider.cs` - NOT NEEDED (soluzione più semplice con middleware custom)

#### 2.6 Static Assets ✅
- [x] Creare `wwwroot/` folder
- [x] Creare `wwwroot/shared.css`
  - [x] Stili comuni a tutti i moduli
  - [x] Variabili CSS per theming Kleios
  - [x] Utility classes responsive
- [x] Configurare static file serving in Program.cs
  - [x] `app.UseStaticFiles()`
- [ ] MudBlazor assets - AUTOMATIC (serviti automaticamente da package)

#### 2.7 Program.cs Setup ✅
- [x] Registrare servizi DI
  - [x] `AddSingleton<IServiceRegistry, InMemoryServiceRegistry>()`
  - [x] `AddSingleton<IWebSocketManager, WebSocketManager>()` (fully qualified)
  - [x] `AddHostedService<ServiceHealthMonitor>()`
  - [x] `AddHttpClient()` per health checks
  - [x] `AddHttpForwarder()` per YARP
- [x] Configurare pipeline middleware
  - [x] UseHttpsRedirection
  - [x] UseStaticFiles
  - [x] UseCors (permissivo per development)
  - [x] **UseWebSockets()** - ⚠️ OBBLIGATORIO per supporto WebSocket
  - [x] MapControllers (PRIMA di UsePrefixRouting)
  - [x] Map WebSocket endpoint `/ws/{serviceName}` (PRIMA di UsePrefixRouting)
  - [x] UsePrefixRouting (custom middleware) - ULTIMO
- [x] Configurare endpoints speciali
  - [x] WebSocket endpoint `/ws/{serviceName}`
  - [x] Health check `/_health` per Gateway
  - [x] Homepage `/` con statistiche
- [x] Logging integrato (ILogger automatico)

**⚠️ ORDINE MIDDLEWARE CRITICO:**
```csharp
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors();
app.UseWebSockets();              // ← DEVE essere qui
app.MapControllers();             // ← Controllers PRIMA
app.Map("/ws/{serviceName}", ...); // ← WebSocket PRIMA
app.UsePrefixRouting();           // ← Routing custom ULTIMO
```
**MOTIVO**: UsePrefixRouting intercetta tutte le richieste non matchate dagli endpoint precedenti. Se WebSocket viene dopo, non riceve mai le richieste.

#### 2.8 Fix WebSocket 400 Error (10 Gennaio 2025) ✅
**Problema**: WebSocket riceveva errore 400 "Bad Request" invece di 101 "Switching Protocols"

**Root Cause**: 
1. ❌ Mancava `app.UseWebSockets()` - necessario per abilitare supporto WebSocket in ASP.NET Core
2. ❌ WebSocket endpoint definito DOPO `UsePrefixRouting()` - veniva intercettato dal middleware di routing prima di processare l'upgrade

**Soluzione**:
- [x] Aggiunto `app.UseWebSockets()` dopo `UseCors()` e prima di tutti gli endpoint
- [x] Spostato `app.Map("/ws/{serviceName}", ...)` PRIMA di `UsePrefixRouting()`
- [x] Spostato `app.MapControllers()` PRIMA di `UsePrefixRouting()`
- [x] Confermato che `PrefixRoutingMiddleware` ignora correttamente `/ws/` paths

**Risultato**: WebSocket connessioni ora accettate correttamente con status 101

---

### FASE 3: Modulo Auth (Primo Modulo) ✅

#### 3.1 Kleios.Module.Auth - Progetto Base ✅
- [x] Creare progetto Blazor Web App
  - [x] `dotnet new blazor -n Kleios.Module.Auth -f net9.0 --interactivity None`
- [x] Aggiungere project references
  - [x] Kleios.Frontend.Shared
  - [x] Kleios.Frontend.Infrastructure (per authentication)
  - [x] Kleios.Frontend.Components
  - [x] Kleios.Shared (per KleiosConstants)
- [x] Aggiungere package references
  - [x] `MudBlazor` (8.13.0)

#### 3.2 Identity e Authentication ✅
- [x] Configurare authentication in Program.cs
  - [x] Chiamare `services.AddKleiosCookieAuthentication(isDevelopment, productionDomain)`
  - [x] Chiamare `services.AddKleiosInfrastructure(configuration)`
  - [x] `services.AddCascadingAuthenticationState()`
  - [x] `services.AddScoped<AuthenticationStateProvider, ServerCookieAuthenticationStateProvider>()` (automatico in AddKleiosInfrastructure)
- [x] Configurare authorization
  - [x] `app.UseAuthentication()` e `app.UseAuthorization()`

#### 3.3 Pages ✅
- [x] Creare `Components/Pages/Account/Login.razor`
  - [x] `@page "/Account/Login"` (path relativo dopo rewrite)
  - [x] `@attribute [AllowAnonymous]`
  - [x] Inject `IAuthenticationService` (da Infrastructure)
  - [x] MudForm con email + password + RememberMe checkbox
  - [x] Query parameter `returnUrl`
  - [x] OnValidSubmit → AuthenticationService.LoginAsync()
  - [x] Se success → Redirect a returnUrl o "/"
  - [x] Se error → MudAlert con messaggio
  - [x] Loading state con MudProgressCircular
- [x] Creare `Components/Pages/Account/Register.razor`
  - [x] `@page "/Account/Register"`
  - [x] `@attribute [AllowAnonymous]`
  - [x] Form con email, password, confirmPassword
  - [x] Validazione client-side con IValidatableObject
  - [x] TODO: RegisterAsync non implementato nel backend (placeholder)
- [x] Creare `Components/Pages/Account/Logout.razor`
  - [x] `@page "/Account/Logout"`
  - [x] OnInitializedAsync → AuthService.LogoutAsync() + redirect al login
  - [x] Loading UI con MudProgressCircular
- [x] Creare `Components/Pages/Account/ForgotPassword.razor`
  - [x] `@page "/Account/ForgotPassword"`
  - [x] Form per richiedere reset password (placeholder)

#### 3.4 App.razor e Routes ✅
- [x] Modificare `Components/App.razor`
  - [x] `<base href="/auth/" />`
  - [x] Link a MudBlazor CSS/JS (_content/MudBlazor)
  - [x] Link a shared.css dal Gateway (https://localhost:5000/shared.css)
  - [x] Link a CSS locale del modulo
- [x] Creare `Components/Routes.razor`
  - [x] CascadingAuthenticationState wrapper
  - [x] Router con AppAssembly
  - [x] AuthorizeRouteView con MainLayout (da Kleios.Frontend.Components)
  - [x] NotAuthorized → RedirectToLogin component
  - [x] NotFound → PageTitle + LayoutView con messaggio

#### 3.5 Gateway Registration ✅
- [x] Implementare `Services/AuthModuleRegistration.cs` (BackgroundService)
  - [x] Delay di 5s prima della registrazione (aspetta Gateway startup)
  - [x] Chiama GatewayConnectionClient.RegisterAsync()
  - [x] ServiceRegistration data:
    - [x] ServiceName: "auth-module"
    - [x] RoutePrefix: "/auth"
    - [x] BaseUrl: da Configuration (ModuleUrl)
    - [x] HealthCheckEndpoint: "/_health"
  - [x] Connessione WebSocket per heartbeat
  - [x] Gestione disconnessione in StopAsync (Dispose client)
- [x] Creare endpoint `/_health`
  - [x] `app.MapGet("/_health", () => Results.Ok(new { status = "healthy", service = "auth-module", timestamp }))`

#### 3.6 Configuration ✅
- [x] `appsettings.json`
  - [x] GatewayUrl: https://localhost:5000
  - [x] ModuleUrl: https://localhost:5001
  - [x] BackendUrl: https://localhost:7000
- [x] `appsettings.Development.json`
  - [x] Same URLs (Aspire override in futuro)

#### 3.7 Middleware ✅
- [x] Implementare `Middleware/PathRewriteMiddleware.cs`
  - [x] Rimuove prefisso /auth dai path in ingresso
  - [x] Gateway inoltra con prefisso, middleware lo rimuove per gestione locale
  - [x] Extension method UsePathRewrite(string prefix)
- [x] Configurare in Program.cs
  - [x] `app.UsePathRewrite("/auth")` PRIMA di UseAuthentication

#### 3.8 Build e Test ✅
- [x] Compilazione riuscita senza errori
- [x] 68 file creati, 60799 insertions
- [x] Commit: 249b390
- [x] Merge a main con --no-ff
- [x] Branch feature/fase-3-module-auth eliminato

**NOTE IMPLEMENTATIVE:**
- Option<T> usa `IsSuccess` property, non `IsSome`
- GatewayConnectionClient richiede HttpClient + ILogger nel costruttore
- PathRewriteMiddleware essenziale per path routing
- _Imports.razor deve includere: Microsoft.AspNetCore.Authorization, Microsoft.AspNetCore.Components.Authorization, Kleios.Frontend.Components.App
- RedirectToLogin component in Kleios.Frontend.Components.App namespace

**TESTING NECESSARIO (FASE 3 completa):**
- [ ] Avviare Gateway su porta 5000
- [ ] Avviare Auth Module su porta 5001
- [ ] Verificare registrazione al Gateway (check /api/_gateway/routes)
- [ ] Testare WebSocket heartbeat (ogni 30s)
- [ ] Verificare routing da Gateway (/auth/Account/Login → Auth Module)
- [ ] Testare path rewriting interno
- [ ] Testare login con backend mock

#### 3.9 Fix Aspire Service Discovery (10 Gennaio 2025) ✅

**Problemi Identificati:**
1. ❌ **InvalidOperationException**: Missing `AddAuthorization()` in Program.cs
2. ❌ **Hardcoded URLs**: appsettings.json con URL fissi invece di Aspire Service Discovery
3. ❌ **Code Duplication**: Layout folder duplicato (MainLayout, NavMenu) nel modulo Auth
4. ❌ **Template Cleanup**: File template (Home, Weather, Error) non rimossi
5. ❌ **AppHost Misconfiguration**: Riferimenti errati ai progetti backend invece dei frontend

**Soluzioni Implementate:**
- [x] **Aggiunto `AddAuthorization()` in Program.cs** → Risolve InvalidOperationException
- [x] **Implementato Aspire Service Discovery**:
  - [x] Aggiunto package: `Microsoft.Extensions.ServiceDiscovery` (9.5.1)
  - [x] Aggiunto package: `Microsoft.Extensions.Http.Resilience` (9.9.0)
  - [x] Chiamata `services.AddServiceDiscovery()` in Program.cs
  - [x] Configurato `ConfigureHttpClientDefaults` con:
    - [x] `AddStandardResilienceHandler()` - Retry automatici e circuit breaker
    - [x] `AddServiceDiscovery()` - Risoluzione automatica servizi
  - [x] Cambiato `backendUrl` da `"https://localhost:7000"` a `"https+http://auth-backend"`
    - ℹ️ Formato Aspire: `https+http://` indica risoluzione automatica con fallback HTTP
- [x] **Rimossi Layout duplicati**: Eliminata cartella `Components/Layout/`
  - [x] MainLayout.razor (usa quello di Kleios.Frontend.Components)
  - [x] MainLayout.razor.css
  - [x] NavMenu.razor (usa quello di Kleios.Frontend.Components)
  - [x] NavMenu.razor.css
- [x] **Rimossi template pages**: 
  - [x] Home.razor
  - [x] Weather.razor
  - [x] Error.razor
- [x] **Aggiornato `appsettings.json`**: Rimossi BackendUrl e ModuleUrl hardcoded
  - ℹ️ GatewayUrl mantenuto come fallback per production
- [x] **Aggiornato `AuthModuleRegistration.cs`**:
  - [x] Injected `IHostEnvironment` per distinguere dev/production
  - [x] ModuleUrl ottenuto dinamicamente da `configuration["urls"]` (Aspire auto-assegna la porta)
  - [x] Fallback a appsettings per production
- [x] **Configurato AppHost correttamente**:
  - [x] Cambiato riferimento da `Kleios_Backend_Authentication` a `Kleios_Module_Auth`
  - [x] Aggiunto `.WithReference(authBackend)` per iniettare service discovery
  - [x] Commentato systemModule (non ancora implementato)
  - [x] Gateway riferisce solo backend e moduli esistenti

**Pattern Aspire Implementato:**
```csharp
// Program.cs - Consuming Service (Auth Module)
builder.Services.AddServiceDiscovery();
builder.Services.ConfigureHttpClientDefaults(http => {
    http.AddStandardResilienceHandler();
    http.AddServiceDiscovery();
});
var backendUrl = "https+http://auth-backend"; // Aspire resolves automatically

// AppHost Program.cs - Service Orchestration
var authBackend = builder.AddProject<Kleios_Backend_Authentication>("auth-backend")
    .WithHttpsEndpoint(name: "auth-backend-https");

var authModule = builder.AddProject<Kleios_Module_Auth>("auth-module")
    .WithHttpsEndpoint(name: "auth-module-https")
    .WithReference(authBackend); // Injects service discovery configuration
```

**Risultato:**
- ✅ Compilazione riuscita senza errori
- ✅ 14 file modificati, 44 insertions, 427 deletions
- ✅ Commit: 6acd82f
- ✅ Merge a main: fix/fase-3-aspire-integration
- ✅ -383 righe di codice duplicato eliminate

**Lezioni Apprese:**
1. **`AddAuthorization()` è OBBLIGATORIO**: Deve essere chiamato nel Program.cs di ogni web app che usa `[Authorize]`
   - ❌ NON può essere astratto in una class library (Infrastructure)
   - ✅ DEVE essere nel Program.cs dell'app host
2. **Aspire Service Discovery > Hardcoded URLs**: 
   - In development: URL risolti dinamicamente via `.WithReference()`
   - In production: Fallback a appsettings.json
3. **DRY Principle**: Layout e componenti condivisi SOLO in Kleios.Frontend.Components
4. **Template Cleanup**: Sempre rimuovere file generati dal template non necessari
5. **Formato URL Aspire**: `"https+http://service-name"` per risoluzione automatica

**Riferimenti Documentazione:**
- [Aspire Service Discovery](https://learn.microsoft.com/en-us/dotnet/aspire/service-discovery/overview)
- [HTTP Client Resilience](https://learn.microsoft.com/en-us/dotnet/core/resilience/)

---

### FASE 4: Modulo Home

#### 4.1 Kleios.Module.Home - Progetto Base
- [ ] Creare progetto Blazor Web App
  - [ ] `dotnet new blazor -n Kleios.Module.Home -f net9.0 --interactivity None`
- [ ] Aggiungere project references
  - [ ] Kleios.Frontend.Shared
  - [ ] Kleios.Frontend.Infrastructure
  - [ ] Kleios.Frontend.Components
  - [ ] Kleios.Shared
- [ ] Aggiungere package references
  - [ ] `MudBlazor`
- [ ] Configurare authentication in Program.cs
  - [ ] `services.AddKleiosCookieAuthentication(isDevelopment, productionDomain)`
  - [ ] `services.AddKleiosInfrastructure(configuration)`

#### 4.2 Pages
- [ ] Creare `Components/Pages/Index.razor`
  - [ ] `@page "/"`
  - [ ] `@attribute [AllowAnonymous]`
  - [ ] Homepage con MudBlazor
  - [ ] Link a Login se non autenticato
  - [ ] Dashboard se autenticato
- [ ] Creare `Components/Pages/Profile.razor`
  - [ ] `@page "/Profile"`
  - [ ] `@attribute [Authorize]`
  - [ ] Visualizza info utente corrente
  - [ ] Form per modificare profilo
  - [ ] MudCard con dati utente

#### 4.3 App.razor e Routes
- [ ] Modificare `Components/App.razor`
  - [ ] `<base href="/" />`
  - [ ] Link a assets condivisi dal Gateway
- [ ] Creare `Components/Routes.razor`
  - [ ] Router con prefix check per `/`
  - [ ] Se route non inizia con nulla (catch-all) gestisci locale
  - [ ] Altrimenti redirect a Gateway

#### 4.4 Gateway Registration
- [ ] Implementare `Services/HomeModuleRegistration.cs`
  - [ ] ServiceName: "home-module"
  - [ ] RoutePrefix: "/"
  - [ ] Priority: lowest (catch-all)
- [ ] Endpoint `/_health`

#### 4.5 Configuration
- [ ] `appsettings.json` con Gateway e Backend URLs

---

### FASE 5: Modulo System

#### 5.1 Kleios.Module.System - Progetto Base
- [ ] Creare progetto Blazor Web App
  - [ ] `dotnet new blazor -n Kleios.Module.System -f net9.0 --interactivity None`
- [ ] Aggiungere project references
  - [ ] Kleios.Frontend.Shared
  - [ ] Kleios.Frontend.Infrastructure
  - [ ] Kleios.Frontend.Components
  - [ ] Kleios.Shared
- [ ] Aggiungere package references
  - [ ] `MudBlazor`
- [ ] Configurare authentication in Program.cs
  - [ ] `services.AddKleiosCookieAuthentication(isDevelopment, productionDomain)`
  - [ ] `services.AddKleiosInfrastructure(configuration)`
  - [ ] Configurare policies personalizzate per admin

#### 5.2 Pages (tutte `[Authorize]`)
- [ ] Creare `Components/Pages/Users.razor`
  - [ ] `@page "/system/Users"`
  - [ ] `@attribute [Authorize]`
  - [ ] MudDataGrid con lista utenti
  - [ ] CRUD operations (create, edit, delete)
- [ ] Creare `Components/Pages/Roles.razor`
  - [ ] `@page "/system/Roles"`
  - [ ] `@attribute [Authorize(Roles = "Admin")]`
  - [ ] Gestione ruoli e permessi
- [ ] Creare `Components/Pages/Settings.razor`
  - [ ] `@page "/system/Settings"`
  - [ ] Configurazioni di sistema
- [ ] Creare `Components/Pages/AuditLog.razor`
  - [ ] `@page "/system/AuditLog"`
  - [ ] Log attività utenti

#### 5.3 App.razor e Routes
- [ ] Modificare `Components/App.razor`
  - [ ] `<base href="/system/" />`
- [ ] Creare `Components/Routes.razor`
  - [ ] Prefix check per `/system`

#### 5.4 Gateway Registration
- [ ] Implementare `Services/SystemModuleRegistration.cs`
  - [ ] ServiceName: "system-module"
  - [ ] RoutePrefix: "/system"
- [ ] Endpoint `/_health`

#### 5.5 Configuration
- [ ] `appsettings.json` con Gateway e Backend URLs

---

### FASE 6: Aspire Integration

#### 6.1 AppHost Configuration
- [ ] Aprire `Orchestration/Kleios.AppHost/Program.cs`
- [ ] Aggiungere project references ai nuovi moduli
  - [ ] `<ProjectReference Include="..\..\Frontend\Kleios.Gateway\Kleios.Gateway.csproj" />`
  - [ ] `<ProjectReference Include="..\..\Frontend\Kleios.Module.Auth\Kleios.Module.Auth.csproj" />`
  - [ ] `<ProjectReference Include="..\..\Frontend\Kleios.Module.Home\Kleios.Module.Home.csproj" />`
  - [ ] `<ProjectReference Include="..\..\Frontend\Kleios.Module.System\Kleios.Module.System.csproj" />`
- [ ] Configurare servizi in Program.cs
  ```csharp
  var gateway = builder.AddProject<Kleios_Gateway>("gateway")
      .WithHttpsEndpoint(port: 5000, name: "gateway-https");
  
  var authModule = builder.AddProject<Kleios_Module_Auth>("auth-module")
      .WithHttpsEndpoint(name: "auth-https")
      .WithReference(authBackend)
      .WithEnvironment("GatewayUrl", gateway.GetEndpoint("gateway-https"));
  
  var homeModule = builder.AddProject<Kleios_Module_Home>("home-module")
      .WithHttpsEndpoint(name: "home-https")
      .WithEnvironment("GatewayUrl", gateway.GetEndpoint("gateway-https"));
  
  var systemModule = builder.AddProject<Kleios_Module_System>("system-module")
      .WithHttpsEndpoint(name: "system-https")
      .WithReference(systemBackend)
      .WithEnvironment("GatewayUrl", gateway.GetEndpoint("gateway-https"));
  ```

#### 6.2 Service Discovery Setup
- [ ] Verificare che Gateway riceva references a tutti i moduli
- [ ] Configurare environment variables per URLs dinamici
- [ ] Test connettività tra servizi

---

### FASE 7: Verifica Backend e Database Esistenti

#### 7.1 Database Verification
- [ ] Verificare database esistente è accessibile
  - [ ] Connection string in `Backend/Kleios.Backend.Authentication/appsettings.json`
  - [ ] Connection string in `Backend/Kleios.Backend.SystemAdmin/appsettings.json`
- [ ] Verificare migrations sono aggiornate
  - [ ] `dotnet ef database update` in Kleios.Database
  - [ ] Nessun pending migration
- [ ] Verificare seed data esistenti
  - [ ] Almeno un admin user disponibile per testing
  - [ ] Ruoli base configurati (Admin, User, etc.)
  - [ ] **Se mancano**: Eseguire seed scripts esistenti
- [ ] Test connessione database
  - [ ] Avviare backend Authentication standalone
  - [ ] Verificare log connessione DB successo

#### 7.2 Backend API Verification
- [ ] **Kleios.Backend.Authentication** - Verificare endpoints
  - [ ] `POST /api/auth/login` esistente e funzionante
    - [ ] Test con Postman: login con admin user
    - [ ] Verificare response contiene JWT
    - [ ] Verificare JWT contiene claims: `sub`, `email`, `roles`
  - [ ] `POST /api/auth/register` esistente (se previsto)
  - [ ] `POST /api/auth/refresh` per refresh token (se previsto)
  - [ ] `POST /api/auth/logout` per invalidare token (se previsto)
  
- [ ] **Kleios.Backend.SystemAdmin** - Verificare endpoints
  - [ ] `GET /api/system/users` - Lista utenti
  - [ ] `GET /api/system/users/{id}` - Dettaglio utente
  - [ ] `POST /api/system/users` - Crea utente
  - [ ] `PUT /api/system/users/{id}` - Aggiorna utente
  - [ ] `DELETE /api/system/users/{id}` - Elimina utente
  - [ ] Endpoint per Roles, Settings, AuditLog (se previsti)

- [ ] **CORS Configuration** - Verificare o aggiungere
  - [ ] Aprire `Backend/Kleios.Backend.SharedInfrastructure/Cors/`
  - [ ] Verificare configurazione CORS esistente
  - [ ] **IMPORTANTE**: Aggiungere Gateway URL agli allowed origins
    - [ ] Development: `https://localhost:5000`
    - [ ] Production: dominio Gateway
  - [ ] Verificare `AllowCredentials = true`
  - [ ] Verificare headers: `Content-Type`, `Authorization`, `Cookie`
  - [ ] **Se CORS non configurato**: Aggiungerlo seguendo pattern esistente

- [ ] **JWT Configuration** - Verificare settings
  - [ ] Verificare `appsettings.json` ha sezione JWT
  - [ ] Secret key configurato (development e production)
  - [ ] Token expiration: verificare è 60 minuti o configurabile
  - [ ] Issuer e Audience configurati (opzionale ma raccomandato)

#### 7.3 Backend Testing
- [ ] Test login flow completo
  - [ ] POST a `/api/auth/login` con credenziali admin
  - [ ] Verificare 200 OK + JWT in response
  - [ ] Decodificare JWT su jwt.io
  - [ ] Verificare claims presenti e corretti
  
- [ ] Test API protette con JWT
  - [ ] GET a `/api/system/users` con header `Authorization: Bearer {jwt}`
  - [ ] Verificare 200 OK + lista utenti
  - [ ] Test senza token → Verificare 401 Unauthorized
  
- [ ] Test CORS
  - [ ] Request da browser con Origin header
  - [ ] Verificare response ha `Access-Control-Allow-Origin`
  - [ ] Verificare `Access-Control-Allow-Credentials: true`

#### 7.4 Aspire Backend Integration
- [ ] Verificare backend sono nel AppHost
  - [ ] `Kleios.Backend.Authentication` referenziato
  - [ ] `Kleios.Backend.SystemAdmin` referenziato
  - [ ] Porte configurate o dinamiche
- [ ] Test avvio backend tramite Aspire
  - [ ] `dotnet run --project Orchestration/Kleios.AppHost`
  - [ ] Verificare backend "Running" nella dashboard
  - [ ] Verificare endpoint raggiungibili

---

### FASE 8: Testing e Debugging

#### 8.1 Unit Tests
- [ ] Test ServiceRegistry.RegisterServiceAsync()
- [ ] Test prefix matching logic
- [ ] Test GatewayConnectionClient retry logic
- [ ] Test cookie authentication flow

#### 8.2 Integration Tests
- [ ] Test Gateway routing: `/auth/Account/Login` → Auth Module
- [ ] Test Gateway routing: `/system/Users` → System Module
- [ ] Test Gateway routing: `/` → Home Module
- [ ] Test 404 handling per route inesistenti
- [ ] Test load balancing (multiple instances)

#### 8.3 Manual Testing
- [ ] Avvio Aspire: `dotnet run --project Orchestration/Kleios.AppHost`
- [ ] Verifica tutti i servizi sono "Running" in dashboard
- [ ] Test login flow:
  - [ ] Accesso a `https://localhost:5000/` → Home
  - [ ] Click "Login" → Redirect a `/auth/Account/Login`
  - [ ] Login con credenziali → Cookie impostato
  - [ ] Redirect a home → Utente autenticato
- [ ] Test navigation:
  - [ ] Da Home a System → Funziona
  - [ ] Da System ad Auth → Funziona
  - [ ] Back button browser → Funziona
- [ ] Test autorizzazione:
  - [ ] Accesso a `/system/Users` senza login → Redirect a login
  - [ ] Login e retry → Accesso consentito
- [ ] Test health monitoring:
  - [ ] Termina un modulo manualmente
  - [ ] Gateway deregistra dopo timeout
  - [ ] Riavvia modulo → Re-registrazione automatica

#### 8.4 Performance Testing
- [ ] Load test con k6/JMeter
- [ ] Verifica cookie overhead
- [ ] Verifica latenza routing Gateway
- [ ] Verifica WebSocket connection stability

---

### FASE 9: Documentation e Cleanup

#### 9.1 Documentazione
- [ ] README.md per ogni progetto
- [ ] Architecture diagram (draw.io o Mermaid)
- [ ] API documentation (Swagger per Backend)
- [ ] Deployment guide

#### 9.2 Code Cleanup
- [ ] Rimuovere vecchio codice Frontend (cartelle obsolete)
- [ ] Code review e refactoring
- [ ] Formattazione codice consistente
- [ ] Aggiungere XML comments

#### 9.3 Configuration
- [ ] Production appsettings.json per ogni modulo
- [ ] Environment variables documentation
- [ ] Secret management (Azure Key Vault o similar)

---

## 🚀 Ordine di Esecuzione Consigliato

1. **Fase 1** - Librerie Condivise (necessarie per tutto il resto)
2. **Fase 2** - Gateway (core dell'architettura)
3. **Fase 3** - Modulo Auth (primo modulo per testare flow completo)
4. **Fase 6.1** - Aspire base (per testare Auth + Gateway)
5. **Fase 8.3** - Test manuale parziale (Gateway + Auth)
6. **Fase 4** - Modulo Home
7. **Fase 5** - Modulo System
8. **Fase 6.2** - Aspire completo
9. **Fase 7** - Backend integration
10. **Fase 8** - Testing completo
11. **Fase 9** - Documentation e cleanup

---

## 📊 Metriche di Successo

- [ ] Tutti i moduli si avviano senza errori
- [ ] Gateway registra tutti i 3 moduli correttamente
- [ ] Routing funziona per tutte le route definite
- [ ] Cookie authentication condiviso tra moduli
- [ ] WebSocket heartbeat stabile (no disconnessioni)
- [ ] Performance: routing < 50ms, page load < 2s
- [ ] Zero errori 404 per route esistenti
- [ ] Navigazione fluida tra moduli
- [ ] Layout e menu condivisi renderizzano correttamente

---

## 🔧 Troubleshooting Common Issues

### Gateway non riceve registrazioni
- Verificare che moduli abbiano `GatewayUrl` configurato correttamente
- Check firewall/antivirus non blocchi port 5000
- Verificare logs del Gateway per errori
- **DEBUG**: Curl al Gateway: `curl https://localhost:5000/api/_gateway/services`

### Cookie non condiviso tra moduli
- Verificare `Domain` del cookie sia corretto (null in dev)
- Verificare `SameSite=Lax` e `Secure=true`
- Check che tutti i moduli usino stesso cookie name
- **DEBUG**: Ispezionare cookie nel browser DevTools (F12 → Application → Cookies)
- **ATTENZIONE**: Se usi `Domain=localhost`, non funziona! DEVE essere `null` in dev

### WebSocket disconnessioni frequenti
- Aumentare timeout nel Gateway
- Verificare che moduli implementino retry logic
- Check network stability
- **DEBUG**: Logs WebSocket nel modulo e Gateway

### Route 404 anche se modulo registrato
- Verificare prefix matching nel Gateway
- Check che page `@page` directive inizi con prefix corretto
- Logs del PrefixRoutingMiddleware
- **DEBUG**: Controllare ServiceRegistry: `GET /api/_gateway/services`

### Static Assets 404 (CSS/JS non caricano)
- Verificare `base href` nel modulo
- Verificare Gateway serva `/wwwroot` correttamente
- **DEBUG**: Ispezionare Network tab nel browser
- **ATTENZIONE**: Path relativo vs assoluto - `/app.css` diventa `{base}/app.css`

### JWT non valido o expired
- Verificare clock sync tra Backend e moduli
- Verificare JWT expiration time (60 min default)
- Verificare claims nel JWT: `https://jwt.io`
- **DEBUG**: Loggare JWT nel ServerCookieAuthenticationStateProvider

### CORS errors dal Backend
- Verificare Origins nella configurazione CORS
- Verificare `AllowCredentials = true`
- Verificare request ha header `Origin`
- **DEBUG**: Browser console mostra errore CORS specifico

### Aspire servizi non si avviano
- Verificare tutti i csproj compilano: `dotnet build Kleios.sln`
- Verificare porte non in conflitto
- Verificare logs dashboard Aspire
- **DEBUG**: Avviare moduli standalone per vedere errori specifici

---

## 📝 Note Finali

- **Priorità**: Completare prima auth flow end-to-end prima di aggiungere features
- **Testing**: Testare dopo ogni fase, non aspettare la fine
- **Logs**: Abilitare verbose logging durante development
- **Backup**: Committare dopo ogni milestone raggiunto

---

## 🎓 Best Practices

### Development Workflow
1. **Branch strategy**: Feature branches per ogni fase
2. **Commit messages**: `[FASE-X.Y] Descrizione` (es: `[FASE-1.1] Implementato ServiceRegistration`)
3. **Testing**: Scrivere test PRIMA di implementare (TDD)
4. **Code review**: Self-review prima di ogni commit

### Performance Tips
- [ ] Usare `IMemoryCache` per route lookups frequenti
- [ ] Configurare HTTP/2 per multiplexing
- [ ] Abilitare response compression nel Gateway
- [ ] Usare `MapStaticAssets()` invece di `UseStaticFiles()` (Blazor 9)

### Security Checklist
- [ ] JWT signature validation abilitata
- [ ] HTTPS obbligatorio in production
- [ ] Cookie HttpOnly + Secure
- [ ] CORS configurato correttamente (no wildcard con credentials)
- [ ] Rate limiting nel Gateway (implementare dopo MVP)
- [ ] Input validation su tutti i form
- [ ] SQL injection prevention (EF Core usa parameterized queries)

### Monitoring e Observability
- [ ] Strutturare logs con `ILogger<T>`
- [ ] Tracciare requests con correlation ID
- [ ] Metriche: response time, error rate, registration count
- [ ] Health checks esposti per tutti i servizi
- [ ] **FUTURE**: Integrare Application Insights o Seq

---

## 📚 Risorse Utili

### Documentazione
- YARP: https://microsoft.github.io/reverse-proxy/
- Blazor SSR: https://learn.microsoft.com/aspnet/core/blazor/
- .NET Aspire: https://learn.microsoft.com/dotnet/aspire/
- MudBlazor: https://mudblazor.com/
- JWT: https://jwt.io/

### Tools
- Postman: Test API Backend
- Browser DevTools: Debug cookie, network, console
- Aspire Dashboard: Monitor services
- Seq: Structured logging (optional)

---

## 🔮 FASE 10: Moduli Futuri ed Estensioni (Post-MVP)

> **NOTA**: Questa fase è da implementare DOPO che le fasi 1-9 sono completate e funzionanti in produzione. Rappresenta la roadmap per espandere il template Kleios con funzionalità enterprise-grade.

---

### 10.1 Backend Gateway Architecture (Discussione Strategica)

#### Opzioni Architetturali
Prima di implementare nuovi moduli backend, è necessario decidere come gestire le API backend:

**Opzione A: Unified Gateway** (Raccomandato per MVP)
- Gateway unico gestisce sia routing frontend che backend API
- YARP routes per `/api/auth`, `/api/system`, `/api/notifications`, etc.
- PRO: Architettura semplice, un solo entry point, CORS centralizzato
- CONTRO: Gateway diventa single point of failure

**Opzione B: Separate Backend Gateway**
- Gateway frontend (routing moduli UI)
- Gateway backend separato (API aggregation, rate limiting, auth)
- PRO: Separazione concerns, scaling indipendente
- CONTRO: Complessità maggiore, doppio hop per chiamate frontend→backend

**Opzione C: Direct Service Discovery** (Aspire native)
- Moduli frontend chiamano backend direttamente via Aspire service discovery
- Gateway solo per routing UI
- PRO: Latenza minima, semplice
- CONTRO: Nessun controllo centralizzato (rate limiting, logging, etc.)

#### Decisione Proposta
- **MVP (Fase 1-9)**: Usare Opzione A (Unified Gateway)
- **Scaling (Fase 10+)**: Migrare a Opzione B se necessario (5+ backend services)

#### Task per Backend Gateway (Future)
- [ ] Valutare necessità di separare frontend/backend gateway
- [ ] Se separato: Implementare `Kleios.Backend.Gateway`
  - [ ] YARP configuration per backend services
  - [ ] API rate limiting (AspNetCoreRateLimit)
  - [ ] JWT validation centralizzato
  - [ ] Request/response logging
  - [ ] Circuit breaker (Polly)
  - [ ] API versioning support
- [ ] Configurare CORS tra frontend e backend gateway
- [ ] Implementare API aggregation (se necessario)
  - [ ] GraphQL gateway (Hot Chocolate) (opzionale)
  - [ ] REST aggregation con caching
- [ ] Load testing per determinare bottleneck

---

### 10.2 Moduli Frontend Aggiuntivi

#### 10.2.1 Notification Module 🔔
**Route Prefix**: `/notifications`  
**Backend Service**: `Kleios.Backend.Notifications`

**Funzionalità**:
- [ ] Toast notifications (MudSnackbar)
- [ ] In-app notification center (bell icon con badge)
- [ ] Lista notifiche con mark as read/unread
- [ ] Filtro per tipo/data
- [ ] Email notifications (invio tramite backend)
- [ ] SignalR per real-time notifications
- [ ] Push notifications (future - service worker)

**Pages**:
- [ ] `/notifications` - Lista notifiche
- [ ] `/notifications/settings` - Preferenze notifiche utente

**Backend Endpoints** (da implementare):
- [ ] `GET /api/notifications` - Lista notifiche utente
- [ ] `POST /api/notifications/mark-read/{id}`
- [ ] `DELETE /api/notifications/{id}`
- [ ] `GET /api/notifications/unread-count`
- [ ] SignalR Hub: `/notificationHub`

**Priority**: ⭐⭐⭐ HIGH (fondamentale per UX)

---

#### 10.2.2 File Management Module 📁
**Route Prefix**: `/files`  
**Backend Service**: `Kleios.Backend.Files`

**Funzionalità**:
- [ ] Upload file con drag & drop (MudFileUpload)
- [ ] Preview file (immagini, PDF con MudBlazor components)
- [ ] Download file con tracking
- [ ] Gestione cartelle virtuali (tree view)
- [ ] Versioning file (history)
- [ ] Quota storage per utente
- [ ] Antivirus scan integration (ClamAV)
- [ ] Thumbnail generation automatico (ImageSharp)

**Pages**:
- [ ] `/files` - File browser
- [ ] `/files/upload` - Upload page
- [ ] `/files/shared` - File condivisi

**Backend Endpoints**:
- [ ] `POST /api/files/upload` (multipart/form-data)
- [ ] `GET /api/files/{id}/download`
- [ ] `GET /api/files/{id}/preview`
- [ ] `DELETE /api/files/{id}`
- [ ] `GET /api/files/folder/{folderId}`

**Storage Options**:
- Azure Blob Storage (production)
- MinIO (self-hosted)
- File system (development)

**Priority**: ⭐⭐⭐ HIGH (molto comune in app enterprise)

---

#### 10.2.3 Communications & Email Templates Module 📧
**Route Prefix**: `/communications`  
**Backend Service**: `Kleios.Backend.Communications`

**Funzionalità**:
- [ ] WYSIWYG editor per email templates (MudRichTextEditor)
- [ ] Variabili dinamiche con placeholder ({{UserName}}, {{Date}}, etc.)
- [ ] Preview email prima di invio
- [ ] Email queue con Hangfire
- [ ] Email tracking (sent/opened/clicked via pixel tracking)
- [ ] Multi-language templates
- [ ] SMS support (Twilio integration) (future)
- [ ] Template versioning

**Pages**:
- [ ] `/communications/templates` - Lista templates
- [ ] `/communications/templates/create` - Editor template
- [ ] `/communications/templates/{id}/edit`
- [ ] `/communications/history` - Cronologia invii

**Backend Endpoints**:
- [ ] `GET /api/communications/templates`
- [ ] `POST /api/communications/templates`
- [ ] `POST /api/communications/send`
- [ ] `GET /api/communications/history`

**Integrations**:
- SendGrid / SMTP per email
- Twilio per SMS (future)

**Priority**: ⭐⭐ MEDIUM (necessario ma dopo MVP)

---

#### 10.2.4 Reporting & Analytics Module 📈
**Route Prefix**: `/reports`  
**Backend Service**: `Kleios.Backend.Reports`

**Funzionalità**:
- [ ] Dashboard configurabili (drag & drop widgets)
- [ ] Report builder con filtri dinamici
- [ ] Export report (PDF via QuestPDF, Excel via EPPlus, CSV)
- [ ] Scheduled reports (Hangfire) via email
- [ ] Chart library (MudBlazor Charts: line, bar, pie, donut)
- [ ] KPI cards con trend indicators
- [ ] Data aggregation con caching (Redis)

**Pages**:
- [ ] `/reports` - Lista report disponibili
- [ ] `/reports/dashboard` - Dashboard utente
- [ ] `/reports/builder` - Report builder visuale
- [ ] `/reports/scheduled` - Report schedulati

**Backend Endpoints**:
- [ ] `GET /api/reports/dashboard-data`
- [ ] `POST /api/reports/generate`
- [ ] `GET /api/reports/{id}/export` (query param: format=pdf|excel|csv)

**Priority**: ⭐⭐ MEDIUM

---

#### 10.2.5 Workflow & Approval System Module ✅
**Route Prefix**: `/workflows`  
**Backend Service**: `Kleios.Backend.Workflows`

**Funzionalità**:
- [ ] Workflow builder visuale (drag & drop nodes)
- [ ] Approval chains configurabili
- [ ] Task assignment con notifiche
- [ ] Email notifications per pending approvals
- [ ] Escalation rules (timeout-based)
- [ ] Audit trail completo per decisioni
- [ ] Delegation support (approva per conto di)

**Pages**:
- [ ] `/workflows` - Lista workflow
- [ ] `/workflows/designer` - Workflow designer
- [ ] `/workflows/tasks` - My tasks
- [ ] `/workflows/pending` - Pending approvals

**Backend Endpoints**:
- [ ] `GET /api/workflows`
- [ ] `POST /api/workflows/start`
- [ ] `POST /api/workflows/approve/{taskId}`
- [ ] `POST /api/workflows/reject/{taskId}`

**Libraries**:
- Elsa Workflows (optional)
- Custom state machine

**Priority**: ⭐ LOW (nice-to-have, use case specifico)

---

#### 10.2.6 Localization & Internationalization Module 🌍
**Route Prefix**: `/localization` (admin only)  
**Backend Service**: Integrato in SharedInfrastructure

**Funzionalità**:
- [ ] UI tradotta in più lingue (IStringLocalizer)
- [ ] Resource editor in-app (admin)
- [ ] Language switcher nel menu utente
- [ ] Date/time formatting per cultura
- [ ] Currency formatting
- [ ] RTL support (arabo, ebraico)
- [ ] Database-backed resources (invece di .resx files)

**Pages**:
- [ ] `/localization/resources` - Lista risorse traducibili
- [ ] `/localization/resources/{key}/edit` - Editor traduzioni

**Implementation**:
- [ ] Estendere `Kleios.Frontend.Infrastructure` con `IStringLocalizer`
- [ ] Creare `Kleios.Backend.Localization` per gestione risorse
- [ ] Database table: `LocalizationResources(Key, Culture, Value)`

**Priority**: ⭐ LOW (dipende da target mercato)

---

#### 10.2.7 Multi-Tenancy Support Module 🏢
**Route Prefix**: `/tenants` (admin only)  
**Backend Service**: Cross-cutting concern (tutti i backend)

**Funzionalità**:
- [ ] Tenant isolation (shared schema con TenantId filter)
- [ ] Tenant registration & provisioning automatico
- [ ] Tenant-specific settings (override global settings)
- [ ] Billing & subscription management
- [ ] Tenant switching per super-admin
- [ ] Tenant branding (logo, colors, domain)

**Pages**:
- [ ] `/tenants` - Lista tenant (super-admin)
- [ ] `/tenants/create` - Provision nuovo tenant
- [ ] `/tenants/{id}/settings` - Configurazione tenant

**Backend Implementation**:
- [ ] Installare `Finbuckle.MultiTenant` package
- [ ] Configurare tenant resolution (header/subdomain/route)
- [ ] Aggiungere `TenantId` a tutte le entities
- [ ] EF Core query filters per tenant isolation
- [ ] Redis cache per tenant context

**Priority**: ⭐ LOW (solo se template è per SaaS)

---

#### 10.2.8 API Integrations Module 🔌
**Route Prefix**: `/integrations` (admin only)  
**Backend Service**: `Kleios.Backend.Integrations`

**Funzionalità**:
- [ ] API key management (CRUD)
- [ ] Webhook listener/dispatcher
- [ ] OAuth provider registration
- [ ] Rate limiting per integration
- [ ] Integration health monitoring
- [ ] Test/sandbox mode per API calls
- [ ] Request/response logging

**Pages**:
- [ ] `/integrations` - Lista integrazioni disponibili
- [ ] `/integrations/{id}/configure` - Configurazione API key
- [ ] `/integrations/webhooks` - Webhook endpoints

**Backend Endpoints**:
- [ ] `GET /api/integrations`
- [ ] `POST /api/integrations/test-connection`
- [ ] `POST /webhooks/{provider}` - Webhook receiver

**Libraries**:
- Refit (typed HTTP clients)
- Polly (retry policies)

**Priority**: ⭐ LOW (dipende da use case)

---

#### 10.2.9 Help & Documentation System Module 📖
**Route Prefix**: `/help`  
**Backend Service**: `Kleios.Backend.Help`

**Funzionalità**:
- [ ] Knowledge base (articoli in Markdown)
- [ ] FAQ section
- [ ] Contextual help (tooltip su ogni pagina)
- [ ] Video tutorials embed (YouTube/Vimeo)
- [ ] Search full-text (Lucene.NET)
- [ ] Feedback su articoli (helpful/not helpful)
- [ ] Version tracking per documentazione

**Pages**:
- [ ] `/help` - Homepage help
- [ ] `/help/article/{id}` - Visualizza articolo
- [ ] `/help/search` - Ricerca articoli
- [ ] `/help/admin/articles` - Gestione articoli (admin)

**Backend Endpoints**:
- [ ] `GET /api/help/articles`
- [ ] `GET /api/help/articles/{id}`
- [ ] `POST /api/help/search`

**Priority**: ⭐⭐ MEDIUM (riduce carico supporto)

---

#### 10.2.10 Theme Customizer Module 🎨
**Route Prefix**: `/branding` (admin/tenant)  
**Backend Service**: `Kleios.Backend.Branding`

**Funzionalità**:
- [ ] Color scheme editor (primary, secondary, etc.)
- [ ] Logo upload per tenant
- [ ] Font selection (Google Fonts integration)
- [ ] CSS customization (custom CSS rules)
- [ ] Live preview delle modifiche
- [ ] Theme versioning
- [ ] Dark/Light mode toggle (user preference)

**Pages**:
- [ ] `/branding` - Theme editor
- [ ] `/branding/preview` - Live preview

**Implementation**:
- [ ] MudBlazor Theming con CSS variables
- [ ] Local storage per user preferences (dark/light)
- [ ] Database storage per tenant branding

**Priority**: ⭐ LOW (nice-to-have per white-label)

---

#### 10.2.11 Background Jobs Dashboard Module ⏱️
**Route Prefix**: `/jobs` (admin only)  
**Backend Service**: Hangfire (già usato)

**Funzionalità**:
- [ ] Job dashboard embedded (Hangfire UI)
- [ ] Cron-based scheduling UI
- [ ] Job history & logs viewer
- [ ] Failed job retry manuale
- [ ] Job queues con priorità
- [ ] Resource monitoring (CPU, RAM)

**Pages**:
- [ ] `/jobs` - Dashboard Hangfire embedded

**Implementation**:
- [ ] Configurare Hangfire Dashboard nel Gateway
- [ ] Authorization filter per admin only
- [ ] Custom storage (SQL Server invece di in-memory)

**Priority**: ⭐⭐ MEDIUM (utile per debugging)

---

#### 10.2.12 Global Search Module 🔍
**Route Prefix**: `/search`  
**Backend Service**: `Kleios.Backend.Search`

**Funzionalità**:
- [ ] Global search bar nel header (shortcut: Ctrl+K)
- [ ] Search across modules (users, files, settings, docs, etc.)
- [ ] Fuzzy search / typo tolerance
- [ ] Search history (local storage)
- [ ] Recent searches
- [ ] Faceted search (filtri per tipo, data, modulo)

**Pages**:
- [ ] `/search?q={query}` - Risultati ricerca

**Implementation**:
- [ ] Elasticsearch / Lucene.NET per full-text search
- [ ] Background service per indexing automatico
- [ ] MudAutocomplete per search bar

**Backend Endpoints**:
- [ ] `GET /api/search?q={query}&filters={...}`
- [ ] `POST /api/search/reindex` (admin only)

**Priority**: ⭐⭐ MEDIUM (se dataset grande)

---

#### 10.2.13 Monitoring Dashboard Module 🏥
**Route Prefix**: `/monitoring` (admin only)  
**Backend Service**: Cross-cutting (tutti i servizi)

**Funzionalità**:
- [ ] Real-time health checks di tutti i servizi
- [ ] Performance metrics (CPU, RAM, response time)
- [ ] Error rate tracking con grafici
- [ ] Database connection status
- [ ] External API status (ping check)
- [ ] Alerts configurabili (email/SMS su threshold)
- [ ] Log viewer integrato (Seq/Serilog)

**Pages**:
- [ ] `/monitoring` - Dashboard overview
- [ ] `/monitoring/services` - Service health details
- [ ] `/monitoring/logs` - Log viewer

**Implementation**:
- [ ] ASP.NET Core Health Checks
- [ ] SignalR per real-time updates
- [ ] Prometheus + Grafana (future - advanced metrics)
- [ ] Application Insights (Azure)

**Priority**: ⭐⭐⭐ HIGH (per production monitoring)

---

### 10.3 Ordine di Implementazione Consigliato (Post-MVP)

**Priority 1 - Must-Have (Entro 3 mesi da MVP)**:
1. ✅ Notification Module (FASE 10.2.1)
2. ✅ File Management Module (FASE 10.2.2)
3. ✅ Monitoring Dashboard (FASE 10.2.13)

**Priority 2 - Should-Have (Entro 6 mesi da MVP)**:
4. Communications Module (FASE 10.2.3)
5. Reporting Module (FASE 10.2.4)
6. Help System (FASE 10.2.9)
7. Background Jobs Dashboard (FASE 10.2.11)

**Priority 3 - Nice-to-Have (Entro 12 mesi da MVP)**:
8. Global Search (FASE 10.2.12)
9. Theme Customizer (FASE 10.2.10)
10. Localization (FASE 10.2.6)

**Priority 4 - Optional (Dipende da use case)**:
11. Workflow & Approvals (FASE 10.2.5)
12. API Integrations (FASE 10.2.8)
13. Multi-Tenancy (FASE 10.2.7)

---

### 10.4 Backend Gateway - Decision Tree

Prima di iniziare l'implementazione dei moduli futuri, valutare:

**Quando implementare Backend Gateway separato?**
- ✅ Hai 5+ backend services
- ✅ Serve rate limiting avanzato per API
- ✅ Serve API aggregation (comporre risposte da più servizi)
- ✅ Serve caching centralizzato per API responses
- ✅ Team separati per frontend e backend

**Quando NON implementare Backend Gateway?**
- ❌ Hai solo 2-3 backend services
- ❌ MVP con pochi utenti
- ❌ Latenza è critica (ogni hop aggiunge 5-10ms)
- ❌ Team piccolo che gestisce tutto

**Architettura Consigliata per Fase 10**:

```
[Utente Browser]
       ↓
[Kleios.Gateway] (Frontend + Backend routing)
       ↓
   ┌───┴──────┬──────────┬──────────┬─────────────┐
   ↓          ↓          ↓          ↓             ↓
[Auth UI] [Home UI] [System UI] [Notif UI] [Files UI]  <- Frontend Modules
   ↓          ↓          ↓          ↓             ↓
[Auth API] [System API] [Notif API] [Files API]       <- Backend Services
       ↓
   [Database]
```

**Future Evolution (se necessario)**:

```
[Utente Browser]
       ↓
[Frontend Gateway] (Solo UI routing)
       ↓
   ┌───┴──────┬──────────┐
   ↓          ↓          ↓
[Auth UI] [Home UI] [System UI]  <- Frontend Modules
   |          |          |
   └──────────┼──────────┘
              ↓
     [Backend Gateway] (API aggregation, rate limiting)
              ↓
   ┌──────────┼──────────┬─────────────┐
   ↓          ↓          ↓             ↓
[Auth API] [System API] [Notif API] [Files API]  <- Backend Services
       ↓
   [Database]
```

---

### 10.5 Template Package Structure (Future)

Obiettivo finale: creare un **NuGet Template Package** per Kleios:

```bash
dotnet new install Kleios.Templates
dotnet new kleios-fullstack -n MyProject
```

**Contenuto Template**:
- [ ] Kleios.Frontend.Shared (library)
- [ ] Kleios.Frontend.Infrastructure (library)
- [ ] Kleios.Frontend.Components (library)
- [ ] Kleios.Gateway (web app)
- [ ] Kleios.Module.Auth (blazor app)
- [ ] Kleios.Module.Home (blazor app)
- [ ] Kleios.Module.System (blazor app)
- [ ] Kleios.Backend.Shared (library)
- [ ] Kleios.Backend.SharedInfrastructure (library)
- [ ] Kleios.Backend.Authentication (web api)
- [ ] Kleios.Backend.SystemAdmin (web api)
- [ ] Kleios.Database (class library con EF Core)
- [ ] Kleios.AppHost (aspire host)
- [ ] Sample data & migrations
- [ ] Docker compose file
- [ ] README.md completo
- [ ] ARCHITECTURE.md
- [ ] CONTRIBUTING.md

**Priority**: ⭐ LOW (solo dopo stabilizzazione architettura)

---

### 10.6 Checklist per Aggiungere Nuovo Modulo

Quando implementi un nuovo modulo in futuro, segui questa checklist:

**Frontend Module**:
- [ ] Creare progetto Blazor Web App (`dotnet new blazor --interactivity None`)
- [ ] Reference a Shared, Infrastructure, Components
- [ ] Configurare `base href` in App.razor
- [ ] Implementare path rewriting middleware in Program.cs
- [ ] Creare Pages con `@page` directive corrette
- [ ] Implementare BackgroundService per Gateway registration
- [ ] Health check endpoint `/_health`
- [ ] `appsettings.json` con GatewayUrl e BackendUrl
- [ ] Testing: standalone + via Gateway

**Backend Service**:
- [ ] Creare progetto Web API (`dotnet new webapi`)
- [ ] Reference a Backend.Shared, Backend.SharedInfrastructure
- [ ] Implementare Controllers con route `/api/{modulename}/...`
- [ ] Configurare CORS per Gateway origin
- [ ] Configurare JWT authentication
- [ ] Health check endpoint
- [ ] Swagger/OpenAPI documentation
- [ ] Unit tests per business logic
- [ ] Integration tests per API endpoints

**Aspire Integration**:
- [ ] Aggiungere project reference in AppHost
- [ ] Configurare `.WithReference()` per dependencies
- [ ] Configurare `.WithEnvironment()` per config injection
- [ ] Test startup e connectivity

**Gateway Configuration**:
- [ ] Aggiornare route prefix list (se necessario)
- [ ] Configurare YARP routes per backend API (se backend gateway)
- [ ] Aggiornare NavMenu per includere nuovo modulo

---

## 📝 Note Finali per Fase 10

⚠️ **IMPORTANTE**: 
- Non iniziare Fase 10 finché Fasi 1-9 non sono **completate, testate e stabili in produzione**
- Ogni nuovo modulo deve essere implementato **incrementalmente** (una feature alla volta)
- Ogni modulo deve avere **test automatizzati** prima di considerarlo completo
- Monitorare **performance impact** di ogni nuovo modulo (non degradare esperienza utente)

🎯 **Obiettivo Fase 10**:
Trasformare Kleios da MVP a **template enterprise-ready** riutilizzabile per qualsiasi progetto aziendale.

---

**Prossimo Step**: Iniziare Fase 1 - Kleios.Frontend.Shared
