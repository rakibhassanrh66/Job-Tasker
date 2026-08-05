// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AssignmentSystem.Infrastructure.Persistence;

/// <summary>
/// Brings the database up to date at startup: applies pending migrations, then optionally
/// seeds demo data. This is what lets `docker compose up` produce a working, populated
/// system with no manual SQL step.
/// </summary>
public static class DatabaseInitializer
{
    public const string SeedFlag = "SEED_ON_STARTUP";

    public static async Task InitialiseAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var logger = provider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DatabaseInitializer).FullName!);

        var db = provider.GetRequiredService<AppDbContext>();
        var configuration = provider.GetRequiredService<IConfiguration>();

        logger.LogInformation("Applying database migrations...");
        await db.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Migrations up to date.");

        // Default to seeding: an evaluator running this for the first time wants demo
        // accounts to exist. The seeder is idempotent, so leaving it on is harmless.
        // Only an explicit "false" turns it off; anything unset or unparseable seeds.
        var configured = configuration[SeedFlag];
        var shouldSeed = string.IsNullOrWhiteSpace(configured)
                         || !bool.TryParse(configured, out var parsed)
                         || parsed;

        if (!shouldSeed)
        {
            logger.LogInformation("{Flag} is false; skipping seed.", SeedFlag);
            return;
        }

        var hasher = provider.GetRequiredService<IPasswordHasher>();
        await DbSeeder.SeedAsync(db, hasher, logger, cancellationToken);
    }
}
