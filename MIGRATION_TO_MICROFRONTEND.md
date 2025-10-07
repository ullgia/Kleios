# 🚀 Migrazione a Microfrontend con Aspire + YARP

## 📋 Panoramica

Questa migrazione trasforma l'architettura da:
- **PRIMA**: `Kleios.Host` monolitico → Backend services
- **DOPO**: `Gateway (YARP)` → `Shell + Module Hosts` → Backend services

### Vantaggi
✅ Sviluppo indipendente per modulo (hot reload solo sul modulo attivo)  
✅ Build più veloci (compili solo il modulo che stai modificando)  
✅ Deploy indipendente (aggiorna solo il modulo cambiato)  
✅ Scalabilità orizzontale (replica solo i moduli sotto carico)  
✅ Team autonomi (ogni team lavora sul proprio modulo)  

### Struttura Target
```
Gateway (YARP - porta 5000)
├── /                    → Kleios.Frontend.Shell (Home, Profile, Layout)
├── /auth/*              → Kleios.Modules.Auth.Host (Login, Register, Reset)
├── /system/*            → Kleios.Modules.System.Host (Users, Roles, Settings)
├── /api/auth/*          → Kleios.Backend.Authentication
└── /api/system/*        → Kleios.Backend.SystemAdmin
```

---

## 📝 CHECKLIST MIGRAZIONE

### ✅ FASE 1: Preparazione (30 min)

#### 1.1 - Backup e Branch
- [ ] Commit di tutti i cambiamenti attuali
- [ ] Crea nuovo branch: `git checkout -b feature/microfrontend-migration`
- [ ] Push del branch: `git push -u origin feature/microfrontend-migration`

#### 1.2 - Verifica Prerequisiti
- [ ] Verifica che tutto compili: `dotnet build Kleios.sln`
- [ ] Verifica che Aspire funzioni: esegui `Kleios.AppHost`
- [ ] Documenta le porte attuali in uso
- [ ] Backup di `appsettings.json` di tutti i progetti

#### 1.3 - Analisi Codice Esistente
- [ ] Identifica componenti in `Kleios.Host/Components/` da migrare
- [ ] Lista le pagine in `Kleios.Host/Components/Pages/`
- [ ] Identifica dipendenze condivise (servizi, modelli)
- [ ] Verifica configurazione autenticazione attuale

---

### ✅ FASE 2: Creazione Progetti Frontend (1-2 ore)

#### 2.1 - Crea Kleios.Frontend.Shell
```powershell
cd Frontend
dotnet new blazorwasm -n Kleios.Frontend.Shell -o Shell/Kleios.Frontend.Shell
cd ..
dotnet sln add Frontend/Shell/Kleios.Frontend.Shell/Kleios.Frontend.Shell.csproj
```

**Checklist progetto Shell:**
- [ ] Progetto creato e aggiunto alla solution
- [ ] Installa MudBlazor: `dotnet add Frontend/Shell/Kleios.Frontend.Shell package MudBlazor`
- [ ] Referenza Kleios.Frontend.Shared: `dotnet add Frontend/Shell/Kleios.Frontend.Shell reference Frontend/Shared/Kleios.Frontend.Shared/Kleios.Frontend.Shared.csproj`
- [ ] Referenza Kleios.Frontend.Components: `dotnet add Frontend/Shell/Kleios.Frontend.Shell reference Frontend/Shared/Kleios.Frontend.Components/Kleios.Frontend.Components.csproj`
- [ ] Configura `TreatWarningsAsErrors` nel .csproj
- [ ] Verifica che compili: `dotnet build Frontend/Shell/Kleios.Frontend.Shell`

**File da creare in Shell:**
- [ ] `Components/Layout/MainLayout.razor` (layout principale con sidebar)
- [ ] `Components/Layout/NavMenu.razor` (menu navigazione con link a moduli)
- [ ] `Components/Layout/UserMenu.razor` (dropdown utente con logout)
- [ ] `Components/Pages/Home.razor` (homepage/dashboard)
- [ ] `Components/Pages/Profile.razor` (profilo utente)
- [ ] `Services/GatewayAuthenticationStateProvider.cs` (auth provider centralizzato)
- [ ] `wwwroot/appsettings.json` (config Gateway URL)

#### 2.2 - Crea Kleios.Modules.Auth.Host
```powershell
cd Frontend/Modules
dotnet new blazorwasm -n Kleios.Modules.Auth.Host -o Kleios.Modules.Auth.Host
cd ../..
dotnet sln add Frontend/Modules/Kleios.Modules.Auth.Host/Kleios.Modules.Auth.Host.csproj
```

**Checklist progetto Auth.Host:**
- [ ] Progetto creato e aggiunto alla solution
- [ ] Installa MudBlazor
- [ ] Referenza `Kleios.Modules.Auth` RCL esistente
- [ ] Referenza `Kleios.Frontend.Shared`
- [ ] Referenza `Kleios.Frontend.Components`
- [ ] Configura `TreatWarningsAsErrors`
- [ ] Verifica che compili

**File da configurare in Auth.Host:**
- [ ] `Program.cs` (servizi auth specifici)
- [ ] `Components/Layout/AuthLayout.razor` (layout minimale per login)
- [ ] `_Imports.razor` (importa namespace Auth RCL)
- [ ] `App.razor` (routing per /auth/*)
- [ ] `wwwroot/appsettings.json` (config backend auth)

#### 2.3 - Crea Kleios.Modules.System.Host
```powershell
cd Frontend/Modules
dotnet new blazorwasm -n Kleios.Modules.System.Host -o Kleios.Modules.System.Host
cd ../..
dotnet sln add Frontend/Modules/Kleios.Modules.System.Host/Kleios.Modules.System.Host.csproj
```

**Checklist progetto System.Host:**
- [ ] Progetto creato e aggiunto alla solution
- [ ] Installa MudBlazor
- [ ] Referenza `Kleios.Modules.System` RCL esistente
- [ ] Referenza `Kleios.Frontend.Shared`
- [ ] Referenza `Kleios.Frontend.Components`
- [ ] Configura `TreatWarningsAsErrors`
- [ ] Verifica che compili

**File da configurare in System.Host:**
- [ ] `Program.cs` (servizi system specifici)
- [ ] `Components/Layout/SystemLayout.razor` (layout per admin panel)
- [ ] `_Imports.razor` (importa namespace System RCL)
- [ ] `App.razor` (routing per /system/*)
- [ ] `wwwroot/appsettings.json` (config backend system)

---

### ✅ FASE 3: Gateway con YARP (1 ora)

#### 3.1 - Crea Progetto Gateway
```powershell
cd Frontend
dotnet new web -n Kleios.Gateway -o Gateway/Kleios.Gateway
cd ..
dotnet sln add Frontend/Gateway/Kleios.Gateway/Kleios.Gateway.csproj
```

**Checklist Gateway:**
- [ ] Progetto creato e aggiunto alla solution
- [ ] Installa YARP: `dotnet add Frontend/Gateway/Kleios.Gateway package Yarp.ReverseProxy`
- [ ] Configura `TreatWarningsAsErrors`

#### 3.2 - Configura YARP Routing
**File da creare/modificare:**
- [ ] `Program.cs` (setup YARP + Authentication)
- [ ] `appsettings.json` (routes e clusters)
- [ ] `appsettings.Development.json` (porte localhost)
- [ ] `appsettings.Production.json` (URL produzione)

**Configurazione routes:**
- [ ] Route `/api/auth/*` → Backend.Authentication
- [ ] Route `/api/system/*` → Backend.SystemAdmin (con auth policy)
- [ ] Route `/auth/*` → Auth.Host
- [ ] Route `/system/*` → System.Host (con auth policy)
- [ ] Route `/*` → Shell (fallback)

#### 3.3 - Test Gateway in Isolamento
- [ ] Avvia Gateway standalone: `dotnet run --project Frontend/Gateway/Kleios.Gateway`
- [ ] Verifica che risponda su `https://localhost:5000`
- [ ] Test con Postman/curl su `/api/auth/login` (dovrebbe dare 404 se backend non è avviato)

---

### ✅ FASE 4: Autenticazione Condivisa (2-3 ore)

#### 4.1 - Backend Authentication
**File da modificare: `Backend/Kleios.Backend.Authentication/Controllers/AuthController.cs`**
- [ ] Endpoint `POST /api/auth/login` con `[AllowAnonymous]`
- [ ] Endpoint `POST /api/auth/register` con `[AllowAnonymous]` (se permetti registrazione)
- [ ] Endpoint `POST /api/auth/forgot-password` con `[AllowAnonymous]`
- [ ] Endpoint `POST /api/auth/reset-password` con `[AllowAnonymous]`
- [ ] Endpoint `GET /api/auth/me` con `[Authorize]` (ritorna info utente corrente)
- [ ] Endpoint `POST /api/auth/logout` con `[Authorize]` (opzionale)
- [ ] Endpoint `POST /api/auth/refresh` con `[AllowAnonymous]` (refresh token)

**File da modificare: `Backend/Kleios.Backend.Authentication/Program.cs`**
- [ ] Configura FallbackPolicy: `RequireAuthenticatedUser()`
- [ ] Configura JWT Bearer con chiave segreta
- [ ] Configura CORS per Gateway: `https://localhost:5000`
- [ ] Aggiungi `app.UseAuthentication()` e `app.UseAuthorization()`

#### 4.2 - GatewayAuthenticationStateProvider
**File: `Frontend/Shell/Kleios.Frontend.Shell/Services/GatewayAuthenticationStateProvider.cs`**
- [ ] Implementa `AuthenticationStateProvider`
- [ ] Metodo `GetAuthenticationStateAsync()` chiama `/api/auth/me`
- [ ] Metodo `MarkUserAsAuthenticated(string token)` salva token
- [ ] Metodo `MarkUserAsLoggedOut()` rimuove token
- [ ] Salva JWT in `localStorage` o cookie HTTP-only
- [ ] HttpClient interceptor aggiunge `Authorization: Bearer {token}` a tutte le richieste

#### 4.3 - Configurazione Auth nei Moduli
**Shell, Auth.Host, System.Host - `Program.cs`:**
- [ ] Registra `GatewayAuthenticationStateProvider` come singleton
- [ ] Registra `AuthenticationStateProvider` come scoped
- [ ] Configura `HttpClient` con BaseAddress al Gateway
- [ ] Aggiungi `AuthorizationMessageHandler` per token

**Test Autenticazione:**
- [ ] Login da Auth.Host salva token
- [ ] Shell carica info utente da `/api/auth/me`
- [ ] System.Host vede utente autenticato
- [ ] Logout rimuove token e reindirizza a login

---

### ✅ FASE 5: Migrazione Componenti (2-3 ore)

#### 5.1 - Identifica Componenti da Migrare
**Da `Kleios.Host/Components/` a destinazione:**
- [ ] `Layout/MainLayout.razor` → `Shell/Components/Layout/MainLayout.razor`
- [ ] `Layout/NavMenu.razor` → `Shell/Components/Layout/NavMenu.razor`
- [ ] `Pages/Home.razor` → `Shell/Components/Pages/Home.razor`
- [ ] `Pages/Profile.razor` → `Shell/Components/Pages/Profile.razor`
- [ ] Componenti auth specifici → già in `Kleios.Modules.Auth` RCL
- [ ] Componenti system specifici → già in `Kleios.Modules.System` RCL

#### 5.2 - Aggiorna Link di Navigazione
**In `Shell/Components/Layout/NavMenu.razor`:**
- [ ] Cambia `/auth/login` → `https://localhost:5000/auth/login` (URL assoluto al Gateway)
- [ ] Cambia `/system/users` → `https://localhost:5000/system/users`
- [ ] Cambia `/system/roles` → `https://localhost:5000/system/roles`
- [ ] Cambia `/system/settings` → `https://localhost:5000/system/settings`

**Oppure usa configurazione:**
```csharp
// Leggi da appsettings.json
var gatewayUrl = builder.Configuration["Gateway:Url"] ?? "https://localhost:5000";
```

#### 5.3 - Aggiorna Chiamate API
**Tutti i moduli devono chiamare il Gateway:**
- [ ] Shell: `httpClient.BaseAddress = new Uri("https://localhost:5000");`
- [ ] Auth.Host: `httpClient.BaseAddress = new Uri("https://localhost:5000");`
- [ ] System.Host: `httpClient.BaseAddress = new Uri("https://localhost:5000");`

**Verifica path API:**
- [ ] Auth: `/api/auth/login`, `/api/auth/me`, etc.
- [ ] System: `/api/system/users`, `/api/system/roles`, `/api/system/settings`

---

### ✅ FASE 6: Aspire Orchestration (1 ora)

#### 6.1 - Aggiorna Kleios.AppHost
**File: `Orchestration/Kleios.AppHost/Program.cs`**

**Aggiungi progetti frontend:**
```csharp
var shell = builder.AddProject<Projects.Kleios_Frontend_Shell>("shell")
    .WithHttpsEndpoint(port: 5200);

var authModule = builder.AddProject<Projects.Kleios_Modules_Auth_Host>("auth-module")
    .WithHttpsEndpoint(port: 5210);

var systemModule = builder.AddProject<Projects.Kleios_Modules_System_Host>("system-module")
    .WithHttpsEndpoint(port: 5220);

var gateway = builder.AddProject<Projects.Kleios_Gateway>("gateway")
    .WithHttpsEndpoint(port: 5000)
    .WithReference(shell)
    .WithReference(authModule)
    .WithReference(systemModule)
    .WithReference(authBackend)
    .WithReference(systemBackend);
```

**Checklist Aspire:**
- [ ] Referenza tutti i progetti frontend nel .csproj di AppHost
- [ ] Configura porte per ogni servizio
- [ ] Gateway ha reference a tutti gli altri servizi
- [ ] Configura environment variables per URLs
- [ ] Test: `dotnet run --project Orchestration/Kleios.AppHost`

#### 6.2 - Configura Service Discovery
**In `appsettings.json` del Gateway:**
```json
{
  "ReverseProxy": {
    "Routes": {
      "shell-route": {
        "ClusterId": "shell-cluster",
        "Match": { "Path": "/{**catch-all}" },
        "Order": 999
      }
    },
    "Clusters": {
      "shell-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "https://{SHELL_SERVICE_URL}" // Aspire risolve automaticamente
          }
        }
      }
    }
  }
}
```

**Oppure usa Service Discovery Aspire:**
- [ ] Installa `Aspire.Hosting.AppHost` in Gateway
- [ ] Usa `builder.Configuration["services:shell:https:0"]` per ottenere URL
- [ ] Configura YARP dinamicamente da Aspire service discovery

---

### ✅ FASE 7: Test Completo (1-2 ore)

#### 7.1 - Test Individuale Moduli
- [ ] **Shell standalone**: Avvia solo Shell, verifica homepage
- [ ] **Auth.Host standalone**: Avvia solo Auth.Host, verifica login page
- [ ] **System.Host standalone**: Avvia solo System.Host, verifica users page (dovrebbe richiedere auth)

#### 7.2 - Test Gateway + Backend
- [ ] Avvia Gateway + Backend.Authentication
- [ ] Test login tramite Gateway: `POST https://localhost:5000/api/auth/login`
- [ ] Verifica che ritorni JWT token
- [ ] Test `/api/auth/me` con token: deve ritornare info utente

#### 7.3 - Test Flusso Completo
**Scenario 1: Login e navigazione**
1. [ ] Apri `https://localhost:5000` (Shell homepage)
2. [ ] Click su "Login" → reindirizza a `https://localhost:5000/auth/login`
3. [ ] Inserisci credenziali e login
4. [ ] Verifica redirect a homepage autenticato
5. [ ] Click su "Users" → naviga a `https://localhost:5000/system/users`
6. [ ] Verifica che carichi lista utenti

**Scenario 2: Protezione route**
1. [ ] Logout
2. [ ] Prova ad accedere a `https://localhost:5000/system/users`
3. [ ] Verifica redirect automatico a login
4. [ ] Login e verifica redirect a `/system/users`

**Scenario 3: Token refresh**
1. [ ] Login
2. [ ] Aspetta scadenza token (o simula)
3. [ ] Verifica che refresh automatico funzioni
4. [ ] Operazioni continuano senza interruzione

#### 7.4 - Test Aspire Dashboard
- [ ] Avvia AppHost: `dotnet run --project Orchestration/Kleios.AppHost`
- [ ] Apri Aspire Dashboard (solitamente `http://localhost:15000`)
- [ ] Verifica che tutti i servizi siano "Running" (verde)
- [ ] Verifica logs di ogni servizio
- [ ] Verifica metriche (CPU, Memory, Requests)

#### 7.5 - Test Hot Reload
**Test indipendenza moduli:**
1. [ ] Avvia tutto tramite Aspire
2. [ ] Modifica un file in `Kleios.Modules.System`
3. [ ] Verifica che SOLO System.Host si ricompili (non Shell, non Auth.Host)
4. [ ] Verifica che la modifica sia visibile senza restart completo

---

### ✅ FASE 8: Sicurezza e CORS (1 ora)

#### 8.1 - Configura CORS nel Gateway
**File: `Frontend/Gateway/Kleios.Gateway/Program.cs`**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontends", policy =>
    {
        policy.WithOrigins(
            "https://localhost:5200", // Shell
            "https://localhost:5210", // Auth.Host
            "https://localhost:5220"  // System.Host
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

app.UseCors("AllowFrontends");
```

**Checklist CORS:**
- [ ] Gateway accetta richieste da Shell, Auth.Host, System.Host
- [ ] Backend accetta richieste SOLO dal Gateway
- [ ] Credenziali (cookies/token) passano correttamente

#### 8.2 - Configura HTTPS e Certificati
**Development:**
- [ ] Trust dev certificates: `dotnet dev-certs https --trust`
- [ ] Verifica che tutti i servizi usino HTTPS
- [ ] Configura redirect HTTP → HTTPS

**Production:**
- [ ] Usa certificati validi (Let's Encrypt, Azure Key Vault)
- [ ] Configura HSTS: `app.UseHsts()`
- [ ] Forza HTTPS: `app.UseHttpsRedirection()`

#### 8.3 - Protezione Backend
**In tutti i backend (`Program.cs`):**
```csharp
builder.Services.AddAuthorization(options =>
{
    // Default: TUTTI gli endpoint richiedono autenticazione
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Solo specifici endpoint con [AllowAnonymous]
```

**Checklist Backend:**
- [ ] FallbackPolicy richiede autenticazione
- [ ] Solo login/forgot-password hanno `[AllowAnonymous]`
- [ ] JWT validato su ogni richiesta
- [ ] CORS permette SOLO Gateway

#### 8.4 - Rate Limiting (opzionale ma consigliato)
**Gateway con rate limiting:**
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
    });
});

app.UseRateLimiter();
```

- [ ] Configura rate limiting per `/api/*`
- [ ] Escludi rate limiting per assets statici
- [ ] Test con tool come Apache Bench

---

### ✅ FASE 9: SignalR (Opzionale - 1-2 ore)

#### 9.1 - Crea Backend Notifications (se serve SignalR)
```powershell
cd Backend
dotnet new web -n Kleios.Backend.Notifications -o Kleios.Backend.Notifications
cd ..
dotnet sln add Backend/Kleios.Backend.Notifications/Kleios.Backend.Notifications.csproj
```

**Checklist Notifications Backend:**
- [ ] Installa `Microsoft.AspNetCore.SignalR`
- [ ] Crea `Hubs/NotificationHub.cs`
- [ ] Configura JWT authentication su WebSocket
- [ ] Test hub standalone

#### 9.2 - Configura YARP per SignalR
**In `Gateway/appsettings.json`:**
```json
{
  "ReverseProxy": {
    "Routes": {
      "signalr-route": {
        "ClusterId": "notifications-cluster",
        "Match": { "Path": "/hubs/{**catch-all}" },
        "Order": 1
      }
    },
    "Clusters": {
      "notifications-cluster": {
        "Destinations": {
          "destination1": { "Address": "https://localhost:5100" }
        }
      }
    }
  }
}
```

**Checklist SignalR Gateway:**
- [ ] Route `/hubs/*` verso backend notifications
- [ ] WebSocket abilitato: `app.UseWebSockets()`
- [ ] Test connessione SignalR da Shell

#### 9.3 - Client SignalR nei Moduli
**In Shell o System.Host:**
```csharp
await using var connection = new HubConnectionBuilder()
    .WithUrl("https://localhost:5000/hubs/notifications", options =>
    {
        options.AccessTokenProvider = () => Task.FromResult(token);
    })
    .Build();

await connection.StartAsync();
```

**Checklist SignalR Client:**
- [ ] HubConnection punta al Gateway (non direttamente al backend)
- [ ] Token JWT passato in AccessTokenProvider
- [ ] Test ricezione notifiche
- [ ] Test reconnect automatico

---

### ✅ FASE 10: Cleanup e Ottimizzazione (30 min)

#### 10.1 - Rimuovi Kleios.Host
**⚠️ ATTENZIONE: Fai questo SOLO dopo aver verificato che tutto funziona!**

- [ ] Verifica che tutti i test passino
- [ ] Backup completo del branch
- [ ] Rimuovi progetto dalla solution: `dotnet sln remove Frontend/Host/Kleios.Host/Kleios.Host.csproj`
- [ ] Elimina cartella: `rm -r Frontend/Host` (o sposta in `/archive`)
- [ ] Rimuovi reference da `Kleios.AppHost`
- [ ] Verifica che solution compili: `dotnet build Kleios.sln`

#### 10.2 - Aggiorna Documentazione
- [ ] Aggiorna `README.md` con nuova architettura
- [ ] Documenta porte usate da ogni servizio
- [ ] Crea diagramma architettura (mermaid o draw.io)
- [ ] Documenta flusso di autenticazione
- [ ] Spiega come avviare singolo modulo in sviluppo

#### 10.3 - Ottimizza Build
**In ogni .csproj:**
```xml
<PropertyGroup>
  <PublishTrimmed>true</PublishTrimmed>
  <PublishSingleFile>false</PublishSingleFile> <!-- Blazor WASM non supporta single file -->
  <BlazorWebAssemblyLoadAllGlobalizationData>false</BlazorWebAssemblyLoadAllGlobalizationData>
</PropertyGroup>
```

- [ ] Abilita trimming per ridurre dimensioni
- [ ] Configura lazy loading per assembly grandi
- [ ] Test dimensioni bundle: `dotnet publish -c Release`

#### 10.4 - Profili di Sviluppo
**Crea profili in Aspire per sviluppo mirato:**
```csharp
// Program.cs in AppHost
var developmentMode = builder.Configuration["DevelopmentMode"];

if (developmentMode == "auth")
{
    // Avvia solo Gateway + Shell + Auth.Host + Backend.Authentication
}
else if (developmentMode == "system")
{
    // Avvia solo Gateway + Shell + System.Host + Backend.SystemAdmin
}
else
{
    // Avvia tutto (default)
}
```

**Checklist profili:**
- [ ] Profilo `auth-dev`: solo modulo auth
- [ ] Profilo `system-dev`: solo modulo system
- [ ] Profilo `full`: tutti i servizi (per test integrazione)
- [ ] Documenta come usare profili: `dotnet run --project Orchestration/Kleios.AppHost -- --DevelopmentMode=auth`

---

### ✅ FASE 11: Deploy e Produzione (2-3 ore)

#### 11.1 - Containerizzazione
**Crea Dockerfile per ogni servizio:**

**Gateway:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Frontend/Gateway/Kleios.Gateway/Kleios.Gateway.csproj", "Frontend/Gateway/Kleios.Gateway/"]
RUN dotnet restore "Frontend/Gateway/Kleios.Gateway/Kleios.Gateway.csproj"
COPY . .
WORKDIR "/src/Frontend/Gateway/Kleios.Gateway"
RUN dotnet build "Kleios.Gateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Kleios.Gateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Kleios.Gateway.dll"]
```

**Checklist Docker:**
- [ ] Dockerfile per Gateway
- [ ] Dockerfile per Shell
- [ ] Dockerfile per Auth.Host
- [ ] Dockerfile per System.Host
- [ ] Dockerfile per Backend.Authentication
- [ ] Dockerfile per Backend.SystemAdmin
- [ ] Test build: `docker build -t kleios-gateway -f Frontend/Gateway/Dockerfile .`
- [ ] Test run: `docker run -p 5000:80 kleios-gateway`

#### 11.2 - Docker Compose (per test locale)
**Crea `docker-compose.yml`:**
```yaml
version: '3.8'

services:
  gateway:
    build:
      context: .
      dockerfile: Frontend/Gateway/Dockerfile
    ports:
      - "5000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ReverseProxy__Clusters__shell-cluster__Destinations__destination1__Address=http://shell:80
    depends_on:
      - shell
      - auth-module
      - system-module

  shell:
    build:
      context: .
      dockerfile: Frontend/Shell/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production

  # ... altri servizi
```

**Checklist Docker Compose:**
- [ ] Configura tutti i servizi
- [ ] Network interno per comunicazione tra container
- [ ] Solo Gateway espone porta pubblica
- [ ] Environment variables per configuration
- [ ] Test: `docker-compose up`

#### 11.3 - Kubernetes / Azure Container Apps
**Se usi orchestrazione cloud:**

**Checklist Kubernetes:**
- [ ] Crea Deployment per ogni servizio
- [ ] Crea Service per ogni Deployment
- [ ] Crea Ingress per Gateway (unico punto di accesso pubblico)
- [ ] Configura HPA (Horizontal Pod Autoscaler) per scaling automatico
- [ ] Configura secrets per JWT key, connection strings
- [ ] Test deploy su cluster di staging

**Checklist Azure Container Apps (con Aspire):**
- [ ] Abilita Azure deployment in AppHost
- [ ] Configura Azure Container Registry
- [ ] Deploy con: `azd deploy` (Aspire gestisce tutto automaticamente)
- [ ] Configura custom domain e SSL
- [ ] Configura Application Insights per monitoring

#### 11.4 - CI/CD Pipeline
**GitHub Actions / Azure DevOps:**

**Checklist Pipeline:**
- [ ] Build automatico su push a `main`
- [ ] Test automatici (unit + integration)
- [ ] Build Docker images
- [ ] Push images a registry (Docker Hub, ACR)
- [ ] Deploy automatico a staging
- [ ] Deploy manuale a produzione (con approval)
- [ ] Rollback automatico se health check fallisce

**Esempio GitHub Actions:**
```yaml
name: Deploy Microfrontend

on:
  push:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 9.0.x
      - name: Build
        run: dotnet build Kleios.sln
      - name: Test
        run: dotnet test Kleios.sln
      - name: Publish Gateway
        run: dotnet publish Frontend/Gateway/Kleios.Gateway -c Release -o ./publish/gateway
      # ... publish altri moduli
      - name: Deploy to Azure
        run: azd deploy
```

---

### ✅ FASE 12: Monitoring e Osservabilità (1 ora)

#### 12.1 - Application Insights (Azure)
**Installa in ogni progetto:**
```powershell
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

**Configura in `Program.cs`:**
```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

**Checklist Monitoring:**
- [ ] Application Insights in Gateway (traccia tutte le richieste)
- [ ] Telemetry nei backend (performance database queries)
- [ ] Custom metrics per business logic (es: login count, user registrations)
- [ ] Alert su errori critici
- [ ] Dashboard per metriche chiave

#### 12.2 - Structured Logging
**In ogni `Program.cs`:**
```csharp
builder.Logging.AddJsonConsole();
builder.Services.AddSerilog((services, config) =>
{
    config.ReadFrom.Configuration(builder.Configuration)
          .ReadFrom.Services(services)
          .Enrich.FromLogContext()
          .WriteTo.Console()
          .WriteTo.ApplicationInsights(services.GetRequiredService<TelemetryConfiguration>(), TelemetryConverter.Traces);
});
```

**Checklist Logging:**
- [ ] Structured logging con Serilog
- [ ] Log correlation ID per tracciare richieste cross-service
- [ ] Log levels configurabili per ambiente (Debug in dev, Warning in prod)
- [ ] Log retention policy (non loggare dati sensibili come password)

#### 12.3 - Health Checks
**In Gateway e backend:**
```csharp
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddUrlGroup(new Uri("https://localhost:5010/health"), name: "auth-backend")
    .AddUrlGroup(new Uri("https://localhost:5020/health"), name: "system-backend");

app.MapHealthChecks("/health");
```

**Checklist Health Checks:**
- [ ] Endpoint `/health` in ogni servizio
- [ ] Gateway controlla health di tutti i backend
- [ ] Kubernetes usa health checks per readiness/liveness
- [ ] Alert se servizio diventa unhealthy

---

## 🎯 RIEPILOGO FINALE

### Totale Tempo Stimato: **12-18 ore**
- Fase 1-2: Preparazione e creazione progetti (2-3 ore)
- Fase 3-4: Gateway e autenticazione (3-4 ore)
- Fase 5-6: Migrazione componenti e Aspire (3-4 ore)
- Fase 7: Test completo (1-2 ore)
- Fase 8-9: Sicurezza e SignalR (2-3 ore)
- Fase 10-12: Cleanup, deploy, monitoring (3-4 ore)

### ✅ Verifica Finale Pre-Merge
- [ ] Tutti i test passano (unit + integration)
- [ ] 0 errori, 0 warning di compilazione
- [ ] Tutti i flussi utente funzionano (login, CRUD, logout)
- [ ] Performance accettabili (hot reload < 2sec, build < 30sec per modulo)
- [ ] Documentazione aggiornata
- [ ] Team ha testato in locale
- [ ] Code review completata

### 📊 Metriche di Successo
- **Build Time**: Da ~2-3 min (monolitico) → ~30 sec per modulo
- **Hot Reload**: Da ~10-15 sec → ~2-3 sec (solo modulo attivo)
- **Deploy Frequency**: Da settimanale → giornaliero (per modulo)
- **Team Velocity**: +30-50% (team lavorano in parallelo senza conflitti)
- **Bundle Size**: Shell ~2MB, ogni modulo ~500KB (vs ~5MB monolitico)

### 🚀 Go Live
1. [ ] Merge branch `feature/microfrontend-migration` → `develop`
2. [ ] Deploy su staging environment
3. [ ] Test smoke completo su staging
4. [ ] Annuncio al team della nuova architettura
5. [ ] Deploy su produzione (graduale: 10% → 50% → 100% utenti)
6. [ ] Monitoring intensivo per prime 48h
7. [ ] Raccolta feedback da team e utenti

---

## 📚 Risorse Utili

- **YARP Documentation**: https://microsoft.github.io/reverse-proxy/
- **Aspire Documentation**: https://learn.microsoft.com/en-us/dotnet/aspire/
- **Blazor WebAssembly**: https://learn.microsoft.com/en-us/aspnet/core/blazor/
- **MudBlazor**: https://mudblazor.com/
- **JWT Best Practices**: https://datatracker.ietf.org/doc/html/rfc8725

---

## 💡 Suggerimenti

### Sviluppo Incrementale
Non cercare di fare tutto in una volta! Segui questa sequenza:
1. **Settimana 1**: Gateway + Shell + Auth (flusso login funzionante)
2. **Settimana 2**: System Module + test completi
3. **Settimana 3**: SignalR (se serve) + ottimizzazioni
4. **Settimana 4**: Deploy produzione + monitoring

### Rollback Plan
Se qualcosa va storto:
1. Non eliminare `Kleios.Host` finché tutto non funziona al 100%
2. Tieni un branch `backup-monolith` con la versione funzionante
3. In caso di emergenza, puoi tornare al monolitico in < 5 minuti

### Comunicazione Team
- Daily standup: aggiorni su progressi migrazione
- Demo settimanale: mostra nuove funzionalità (hot reload, deploy indipendente)
- Documentazione wiki: guida "Come sviluppare un nuovo modulo"

---

**Buona migrazione! 🚀**
