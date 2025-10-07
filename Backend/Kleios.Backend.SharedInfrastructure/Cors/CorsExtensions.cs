using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kleios.Backend.SharedInfrastructure.Cors;

/// <summary>
/// Estensioni per la configurazione centralizzata di CORS
/// </summary>
public static class CorsExtensions
{
    /// <summary>
    /// Aggiunge CORS configurato da appsettings.json per i microservizi Kleios
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>IServiceCollection per chaining</returns>
    public static IServiceCollection AddKleiosCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var corsSection = configuration.GetSection("Cors");
        var policyName = corsSection["PolicyName"] ?? "KleiosPolicy";
        var allowedOrigins = corsSection.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            options.AddPolicy(policyName, policy =>
            {
                // Origins
                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins);
                }
                else
                {
                    policy.AllowAnyOrigin();
                }

                // Credentials
                var allowCredentials = corsSection.GetValue<bool>("AllowCredentials");
                if (allowCredentials)
                {
                    policy.AllowCredentials();
                }

                // Methods
                var allowedMethods = corsSection.GetSection("AllowedMethods").Get<string[]>();
                if (allowedMethods?.Length > 0)
                {
                    policy.WithMethods(allowedMethods);
                }
                else
                {
                    policy.AllowAnyMethod();
                }

                // Headers
                var allowedHeaders = corsSection.GetSection("AllowedHeaders").Get<string[]>();
                if (allowedHeaders?.Length > 0 && !allowedHeaders.Contains("*"))
                {
                    policy.WithHeaders(allowedHeaders);
                }
                else
                {
                    policy.AllowAnyHeader();
                }

                // Max Age
                var maxAge = corsSection.GetValue<int>("MaxAge");
                if (maxAge > 0)
                {
                    policy.SetPreflightMaxAge(TimeSpan.FromSeconds(maxAge));
                }
            });
        });

        return services;
    }

    /// <summary>
    /// Ottiene il nome della policy CORS da configurazione
    /// </summary>
    /// <param name="configuration">Configuration</param>
    /// <returns>Nome della policy CORS</returns>
    public static string GetCorsPolicy(this IConfiguration configuration)
    {
        return configuration.GetSection("Cors")["PolicyName"] ?? "KleiosPolicy";
    }
}
