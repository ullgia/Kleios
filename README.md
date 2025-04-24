# Kleios

Kleios is a modern .NET application with a microservices-based architecture that uses Blazor for the frontend and ASP.NET Core for backend services.

## Project Structure

The solution is organized into the following main areas:

### Frontend
- **Host**: The main Blazor application that uses server-side interactive components
- **Modules**: Reusable modules with specific functionality
  - **Auth**: Module for authentication and authorization management
  - **System**: Module for system administration and configuration
- **Infrastructure**: Shared infrastructure components for frontend services
- **Shared**: Common components, models, and utilities used across frontend modules

### Backend
- **Authentication**: Dedicated API service for user authentication
- **System**: API service for system administration, logs, and settings
- **Database**: Data access layer and entity models
- **Shared**: Common utilities and models for backend services

### Orchestration
- **AppHost**: Orchestration host for microservices based on .NET Aspire
- **ServiceDefaults**: Shared default configurations for services

## Technologies Used

- **.NET 9.0**: Main development framework
- **Blazor**: For frontend development with interactive components
- **MudBlazor**: UI component library for Blazor
- **.NET Aspire**: For microservices orchestration
- **ASP.NET Core**: For backend API development
- **Entity Framework Core**: For data access and ORM
- **ZiggyCreatures.Caching.Fusion**: Advanced caching solution for performance optimization
- **JWT Authentication**: For secure API access
- **OpenID Connect**: For identity management

## Technical Architecture

### Architecture Overview

Kleios implements a modern microservices architecture, where each service is responsible for a specific domain of the application. This separation allows:

- Independent development, testing, and deployment of services
- Scaling components based on specific load
- Fault isolation and increased resilience
- Adopting different technologies for different services when necessary

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

### Authentication and Authorization System

Kleios implements a robust authentication and authorization system:

- **Cookie-based authentication**: For secure user session management
- **JWT with refresh tokens**: For API authentication and automatic session renewal
- **Granular authorization policies**: Based on roles and specific permissions
- **Centralized user management**: With support for registration, login, and profile management
- **Token Distribution Service**: Manages token issuance and validation across services
- **Smart Caching**: Optimizes token validation with fusion caching strategy

The system uses .NET Core's Claims-based Identity pattern and implements:
- HTTP interceptors for automatic token management
- Custom provider for authentication state in Blazor
- Transparent token renewal mechanisms
- Security hardening with anti-forgery protection

### Inter-Service Communication

Services communicate with each other using:

- **HTTP/REST**: For most synchronous communications
- **Centralized error handling**: Through the custom `Result<T>` type
- **Automatic endpoint configuration**: Via .NET Aspire
- **Resilient HTTP clients**: With retry policies and circuit breakers

## Requirements

- **.NET 9.0 SDK**
- **Visual Studio 2022** (version 17.10 or higher) or **Visual Studio Code** with C# extensions
- **Docker Desktop** (optional, for containerized execution)

## Development Environment Configuration

### Prerequisites

1. Install .NET 9.0 SDK from the [official download page](https://dotnet.microsoft.com/download)
2. If using Visual Studio, make sure you have installed the workloads:
   - ASP.NET and web development
   - Azure development
   - .NET multi-platform app development

### Initial Setup

1. Clone the repository
   ```bash
   git clone https://github.com/your-username/Kleios.git
   cd Kleios
   ```

2. Restore NuGet packages
   ```bash
   dotnet restore
   ```

3. Configure application settings
   - Copy and rename the `appsettings.example.json` files to `appsettings.Development.json` in each project
   - Configure database connection strings and other necessary settings

### Starting the Application

To start the application in development mode:

1. Set `Kleios.AppHost` as the startup project
2. Press F5 or start debugging

This will start the orchestration of all necessary services through .NET Aspire, which:
- Starts all microservices in parallel
- Automatically configures URLs and ports
- Configures service discovery
- Provides a dashboard for service monitoring

## Module Structure

Kleios follows a modular architecture where each main functionality is isolated in a specific module. This approach allows:

- Independent development of features
- Component reuse
- Separation of concerns
- Improved maintainability

### Design Patterns Used

- **Clean Architecture**: Separation of responsibilities in layers
- **CQRS (Command Query Responsibility Segregation)**: For complex operations
- **Repository Pattern**: For data access
- **Dependency Injection**: For loose coupling between components
- **Result Pattern**: For unified error handling
- **Options Pattern**: For configuration management
- **Mediator Pattern**: For decoupling request handlers

## Error Handling and Logging

Kleios implements a centralized error handling system that:

- Captures and logs exceptions at all levels of the application
- Provides consistent error responses through the `Result<T>` type
- Uses structured logging to facilitate analysis
- Centralizes log viewing and management through the System service

## Deployment

### Test Environment

For deployment in a test environment:

1. Use the `dotnet publish` command with the appropriate configuration
2. Implement CI/CD with GitHub Actions or Azure DevOps
3. Monitor performance and logs through the Aspire dashboard

### Production Environment

For production environments it is recommended:

1. Using Docker containers orchestrated with Kubernetes
2. Implementing resilience strategies like Circuit Breaker
3. Configuring advanced monitoring with Prometheus and Grafana
4. Using an API Gateway to manage external traffic

## Roadmap

- **Q2 2025**: Implementation of authentication with external providers (Google, Microsoft)
- **Q3 2025**: Advanced administration dashboard
- **Q4 2025**: Support for custom themes and white-labeling
- **Q1 2026**: Public APIs with OpenAPI documentation

## Troubleshooting

### Common Issues

#### Authentication Errors
- Verify that cookies are enabled in the browser
- Check the validity and expiration of refresh tokens
- Verify that CORS policies are properly configured

#### Connection Issues Between Services
- Verify that all services are running from the Aspire dashboard
- Check logs for communication errors
- Verify that network configurations allow communication between services

## Contributing to the Project

To contribute to the project, follow these steps:

1. Create a fork of the repository
2. Create a branch for your feature (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add a new feature'`)
4. Push the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Code Guidelines

- Follow C# naming conventions
- Add unit tests for new features
- Document APIs and complex functionality
- Use the Result pattern for error handling

## License

[Insert your license here]

---

*This README was last updated on April 24, 2025*