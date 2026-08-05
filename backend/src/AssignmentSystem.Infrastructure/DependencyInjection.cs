// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Infrastructure.Auth;
using AssignmentSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssignmentSystem.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Default";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured. " +
                "Copy .env.example to .env and set ConnectionStrings__Default.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        // The Application layer depends on the interface; both resolve to the same scoped
        // instance so a service and the authorizer share one unit of work.
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IPasswordHasher, PasswordHasherAdapter>();
        services.AddScoped<ITokenService, JwtTokenService>();

        return services;
    }

    /// <summary>
    /// Reads and validates the JWT settings during startup, so a missing or too-short
    /// signing key stops the application immediately with a clear message rather than
    /// surfacing later as tokens that cannot be validated.
    /// </summary>
    public static JwtOptions GetValidatedJwtOptions(this IConfiguration configuration)
    {
        var options = new JwtOptions();
        configuration.GetSection(JwtOptions.SectionName).Bind(options);
        options.Validate();

        return options;
    }
}
