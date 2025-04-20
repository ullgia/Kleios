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

## Requisiti

- .NET 9.0 SDK
- Visual Studio 2022 o superiore / Visual Studio Code con le estensioni C#

## Come Iniziare

### Configurazione dell'Ambiente di Sviluppo

1. Clona il repository
2. Apri la soluzione `Kleios.sln` in Visual Studio
3. Assicurati di avere installato .NET 9.0 SDK
4. Ripristina i pacchetti NuGet

### Avvio dell'Applicazione

Per avviare l'applicazione in modalità sviluppo:

1. Imposta `Kleios.AppHost` come progetto di avvio
2. Premi F5 o avvia il debug

Questo avvierà l'orchestrazione di tutti i servizi necessari.

## Struttura dei Moduli

Kleios segue un'architettura modulare dove ogni funzionalità principale è isolata in un modulo specifico. Questo approccio consente:

- Sviluppo indipendente delle funzionalità
- Riutilizzo dei componenti
- Separazione delle responsabilità
- Manutenibilità migliorata

## Contribuire al Progetto

Per contribuire al progetto, segui questi passaggi:

1. Crea un fork del repository
2. Crea un branch per la tua funzionalità (`git checkout -b feature/amazing-feature`)
3. Committa le tue modifiche (`git commit -m 'Aggiunta una nuova funzionalità'`)
4. Pusha il branch (`git push origin feature/amazing-feature`)
5. Apri una Pull Request

## Licenza

[Inserisci la tua licenza qui]

---

*Questo README è stato generato il 18 aprile 2025*