// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Infrastructure.Auth;
using AssignmentSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace AssignmentSystem.IntegrationTests.Infrastructure;

/// <summary>
/// A real PostgreSQL instance for the test run, started in Docker and thrown away after.
///
/// These tests assert things only a real database can enforce — unique indexes, check
/// constraints, foreign keys. The EF in-memory provider silently ignores all three, so a
/// suite built on it would pass while the actual schema was broken.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("assignment_system_test")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public IPasswordHasher PasswordHasher { get; } = new PasswordHasherAdapter();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Apply the real migrations rather than EnsureCreated, so what the tests run
        // against is exactly what an evaluator's database will be built from.
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new AppDbContext(options);
    }
}

/// <summary>
/// Shares one container across every test class in the collection — starting a
/// container per class would dominate the run time.
/// </summary>
[CollectionDefinition(Name)]
public class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
