using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace Kleios.Backend.SharedInfrastructure.Swagger;

/// <summary>
/// Estensioni per la configurazione centralizzata di Swagger/OpenAPI
/// </summary>
public static class SwaggerExtensions
{
    /// <summary>
    /// Aggiunge Swagger con configurazione JWT per i microservizi Kleios
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="serviceName">Nome del servizio (es: "Authentication", "System Admin")</param>
    /// <param name="serviceDescription">Descrizione del servizio</param>
    /// <param name="version">Versione API (default: "v1")</param>
    /// <returns>IServiceCollection per chaining</returns>
    public static IServiceCollection AddKleiosSwagger(
        this IServiceCollection services,
        string serviceName,
        string serviceDescription,
        string version = "v1")
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            // Configurazione documento OpenAPI
            options.SwaggerDoc(version, new OpenApiInfo
            {
                Title = $"Kleios {serviceName} API",
                Version = version,
                Description = serviceDescription,
                Contact = new OpenApiContact
                {
                    Name = "Kleios Team",
                    Email = "support@kleios.com"
                }
            });

            // JWT Bearer Authentication
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Inserisci il token JWT nel formato: Bearer {token}"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Auto-include XML comments dal calling assembly
            var callingAssembly = Assembly.GetCallingAssembly();
            var xmlFile = $"{callingAssembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }

    /// <summary>
    /// Configura Swagger UI per i microservizi Kleios (solo Development)
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <param name="serviceName">Nome del servizio per il titolo</param>
    /// <param name="version">Versione API (default: "v1")</param>
    /// <returns>IApplicationBuilder per chaining</returns>
    public static IApplicationBuilder UseKleiosSwaggerUI(
        this IApplicationBuilder app,
        string serviceName,
        string version = "v1")
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint($"/swagger/{version}/swagger.json", $"Kleios {serviceName} API {version}");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = $"Kleios {serviceName} API";
            options.EnableDeepLinking();
            options.DisplayRequestDuration();
        });

        return app;
    }
}
