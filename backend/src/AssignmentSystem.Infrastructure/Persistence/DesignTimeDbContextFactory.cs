// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AssignmentSystem.Infrastructure.Persistence;

/// <summary>
/// Used only by `dotnet ef` at design time.
///
/// Without this, generating a migration would require the API host to start, which means
/// a valid configuration and a reachable database just to scaffold DDL. This keeps
/// migration authoring independent of application startup — the connection string here
/// is never opened for `migrations add`, only for commands that actually touch the
/// database.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=assignment_system;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
