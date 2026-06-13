using System.Text.Json.Serialization;
using Scalar.AspNetCore;
using Serilog;
using {{SolutionName}}.Api.Middleware;

namespace {{SolutionName}}.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddOpenApi();
        services.AddCors(options =>
        {
            options.AddPolicy("Web", policy =>
                policy.WithOrigins(configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"])
                    .AllowAnyHeader()
                    .AllowAnyMethod());
        });
        services.AddHealthChecks();

        // EXTEND: Api-layer registrations for the chosen stack go here
        // (authentication/authorization, dashboards, SignalR, rate limiting...).

        return services;
    }

    public static WebApplication UseApi(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            // Tool UIs are exposed in Development: the OpenAPI document and Scalar.
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.UseCors("Web");

        // EXTEND: UseAuthentication()/UseAuthorization() and tool dashboards with a
        // UI (e.g. Hangfire) go here - dashboards must be reachable in Development.

        app.MapHealthChecks("/health");

        // EXTEND: Map{Feature}Endpoints() calls go here.

        return app;
    }
}
