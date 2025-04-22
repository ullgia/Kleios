# Kleios

Kleios è un'applicazione .NET moderna con un'architettura basata su microservizi che utilizza Blazor per il frontend e ASP.NET Core per i servizi backend.

## Struttura del Progetto

La soluzione è organizzata nelle seguenti aree principali:

### Frontend
- **Host**: L'applicazione Blazor principale che utilizza componenti interattivi lato server
- **Modules**: Moduli riutilizzabili con funzionalità specifiche
  - **Auth**: Modulo per la gestione dell'autenticazione e autorizzazione

### Backend
- **Authentication**: Servizio API dedicato alla gestione dell'autenticazione degli utenti
- **LogsSettings**: Servizio API dedicato alla gestione dei log e delle impostazioni di sistema

### Orchestration
- **AppHost**: Host di orchestrazione per i microservizi basato su .NET Aspire
- **ServiceDefaults**: Configurazioni predefinite condivise tra i servizi

## Tecnologie Utilizzate

- **.NET 9.0**: Framework principale per lo sviluppo
- **Blazor**: Per lo sviluppo del frontend con componenti interattivi
- **MudBlazor**: Libreria di componenti UI per Blazor
- **.NET Aspire**: Per l'orchestrazione dei microservizi
- **ASP.NET Core**: Per lo sviluppo delle API backend

## Architettura Tecnica

### Panoramica dell'Architettura

Kleios implementa un'architettura a microservizi moderna, dove ogni servizio è responsabile per un dominio specifico dell'applicazione. Questa separazione permette di:

- Sviluppare, testare e deployare i servizi in modo indipendente
- Scalare i componenti in base al carico specifico
- Isolamento dei fault e maggiore resilienza
- Adottare tecnologie diverse per servizi diversi quando necessario

```
┌─────────────────┐     ┌──────────────────────┐
│                 │     │                      │
│  Blazor Frontend│     │  .NET Aspire AppHost │
│                 │     │                      │
└────────┬────────┘     └──────────┬───────────┘
         │                         │
         ▼                         ▼
┌─────────────────────────────────────────────┐
│                                             │
│              Service Discovery              │
│                                             │
└───────┬─────────────────┬─────────────┬─────┘
        │                 │             │
        ▼                 ▼             ▼
┌──────────────┐  ┌──────────────┐ ┌──────────────┐
│              │  │              │ │              │
│   Auth API   │  │   Logs API   │ │  Other APIs  │
│              │  │              │ │              │
└──────────────┘  └──────────────┘ └──────────────┘
```

### Sistema di Autenticazione e Autorizzazione

Kleios implementa un sistema di autenticazione e autorizzazione robusto:

- **Autenticazione basata su cookie**: Per una sicura gestione delle sessioni utente
- **JWT con refresh token**: Per l'autenticazione nelle API e il rinnovo automatico delle sessioni
- **Policy di autorizzazione granulari**: Basate su ruoli e permessi specifici
- **Gestione centralizzata degli utenti**: Con supporto per registrazione, login e gestione profili

Il sistema utilizza il pattern Claims-based Identity di .NET Core e implementa:
- Interceptor HTTP per la gestione automatica dei token
- Provider personalizzato per lo stato di autenticazione in Blazor
- Meccanismi di rinnovo trasparente dei token

### Comunicazione tra Servizi

I servizi comunicano tra loro utilizzando:

- **HTTP/REST**: Per la maggior parte delle comunicazioni sincrone
- **Gestione centralizzata degli errori**: Attraverso il tipo `Result<T>` personalizzato
- **Configurazione automatica degli endpoint**: Tramite .NET Aspire

## Requisiti

- **.NET 9.0 SDK**
- **Visual Studio 2022** (versione 17.10 o superiore) o **Visual Studio Code** con le estensioni C#
- **Docker Desktop** (opzionale, per esecuzione containerizzata)

## Configurazione dell'Ambiente di Sviluppo

### Prerequisiti

1. Installa .NET 9.0 SDK dalla [pagina ufficiale di download](https://dotnet.microsoft.com/download)
2. Se utilizzi Visual Studio, assicurati di avere installato i carichi di lavoro:
   - Sviluppo ASP.NET e web
   - Sviluppo di Azure
   - Sviluppo multipiattaforma .NET

### Setup Iniziale

1. Clona il repository
   ```bash
   git clone https://github.com/tuo-username/Kleios.git
   cd Kleios
   ```

2. Ripristina i pacchetti NuGet
   ```bash
   dotnet restore
   ```

3. Configura le impostazioni dell'applicazione
   - Copia e rinomina i file `appsettings.example.json` in `appsettings.Development.json` in ciascun progetto
   - Configura le stringhe di connessione al database e altre impostazioni necessarie

### Avvio dell'Applicazione

Per avviare l'applicazione in modalità sviluppo:

1. Imposta `Kleios.AppHost` come progetto di avvio
2. Premi F5 o avvia il debug

Questo avvierà l'orchestrazione di tutti i servizi necessari attraverso .NET Aspire, che:
- Avvia tutti i microservizi in parallelo
- Configura automaticamente le URL e le porte
- Configura la service discovery
- Fornisce un dashboard per il monitoraggio dei servizi

## Struttura dei Moduli

Kleios segue un'architettura modulare dove ogni funzionalità principale è isolata in un modulo specifico. Questo approccio consente:

- Sviluppo indipendente delle funzionalità
- Riutilizzo dei componenti
- Separazione delle responsabilità
- Manutenibilità migliorata

### Pattern di Progettazione Utilizzati

- **Clean Architecture**: Separazione delle responsabilità in layer
- **CQRS (Command Query Responsibility Segregation)**: Per alcune operazioni complesse
- **Repository Pattern**: Per l'accesso ai dati
- **Dependency Injection**: Per accoppiamento debole tra componenti
- **Result Pattern**: Per la gestione unificata degli errori

## Gestione degli Errori e Logging

Kleios implementa un sistema centralizzato di gestione degli errori che:

- Cattura e registra eccezioni in tutti i livelli dell'applicazione
- Fornisce risposte di errore consistenti attraverso il tipo `Result<T>`
- Utilizza logging strutturato per facilitare l'analisi
- Centralizza la visualizzazione e la gestione dei log attraverso il servizio LogsSettings

## Deployment

### Ambiente di Test

Per il deployment in ambiente di test:

1. Utilizza il comando `dotnet publish` con la configurazione appropriata
2. Implementa CI/CD con GitHub Actions o Azure DevOps
3. Monitora le prestazioni e i log attraverso il dashboard di Aspire

### Ambiente di Produzione

Per ambienti di produzione si raccomanda:

1. Utilizzo di container Docker orchestrati con Kubernetes
2. Implementazione di strategie di resilienza come Circuit Breaker
3. Configurazione di monitoring avanzato con Prometheus e Grafana
4. Utilizzo di un API Gateway per gestire il traffico esterno

## Roadmap

- **Q2 2025**: Implementazione di autenticazione con provider esterni (Google, Microsoft)
- **Q3 2025**: Dashboard di amministrazione avanzata
- **Q4 2025**: Supporto per temi personalizzati e white-labeling
- **Q1 2026**: API pubbliche con documentazione OpenAPI

## Troubleshooting

### Problemi Comuni

#### Errori di Autenticazione
- Verifica che i cookie siano abilitati nel browser
- Controlla la validità e la scadenza dei token di refresh
- Verifica che le policy di CORS siano configurate correttamente

#### Problemi di Connessione tra Servizi
- Verifica che tutti i servizi siano in esecuzione dal dashboard di Aspire
- Controlla i log per errori di comunicazione
- Verifica che le configurazioni di rete permettano la comunicazione tra i servizi

## Contribuire al Progetto

Per contribuire al progetto, segui questi passaggi:

1. Crea un fork del repository
2. Crea un branch per la tua funzionalità (`git checkout -b feature/amazing-feature`)
3. Committa le tue modifiche (`git commit -m 'Aggiunta una nuova funzionalità'`)
4. Pusha il branch (`git push origin feature/amazing-feature`)
5. Apri una Pull Request

### Linee Guida per il Codice

- Segui le convenzioni di naming di C#
- Aggiungi test unitari per le nuove funzionalità
- Documenta le API e le funzionalità complesse
- Utilizza il pattern Result per la gestione degli errori

## Licenza

[Inserisci la tua licenza qui]

---

*Questo README è stato aggiornato il 21 aprile 2025*