// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace AssignmentSystem.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real application against a throwaway PostgreSQL container and drives it over
/// HTTP. Requests go through the genuine pipeline — authentication, authorization, the
/// exception middleware, the validation filter — so what these tests observe is what a
/// caller would observe.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("assignment_system_api_test")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .Build();

    /// <summary>Requests-per-window for the auth rate limiter. Raised well above the
    /// production default here so ordinary tests can log in freely; the limiter itself is
    /// exercised by <see cref="RateLimitedApiFactory"/> with a deliberately tiny limit.</summary>
    protected virtual int AuthPermitPerWindow => 1000;

    /// <summary>Failed logins allowed before the per-account lockout engages. Raised here so
    /// ordinary tests can exercise wrong-password paths without ever locking a seeded
    /// account; the lockout itself is exercised by <see cref="ThrottledApiFactory"/>.</summary>
    protected virtual int LoginMaxFailures => 1000;

    /// <summary>Login attempts allowed per account per window. Raised here for the same
    /// reason as <see cref="LoginMaxFailures"/>.</summary>
    protected virtual int AuthAccountPermitPerWindow => 1000;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Set as environment variables, not through ConfigureAppConfiguration.
        //
        // Program.cs reads the connection string and the JWT settings while composing the
        // service collection, which happens before the host is built — and the factory's
        // configuration callbacks only run during Build(). Environment variables are read
        // by CreateBuilder at the very start, so they are the only override that lands in
        // time. DotEnvLoader deliberately never overwrites an existing variable, so a
        // developer's local .env cannot leak into a test run.
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Default", _container.GetConnectionString());
        Environment.SetEnvironmentVariable(
            "Jwt__Key", "integration-test-signing-key-long-enough-for-hmac-sha256");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "AssignmentSystem.Api");
        Environment.SetEnvironmentVariable("Jwt__Audience", "AssignmentSystem.Client");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenMinutes", "15");
        Environment.SetEnvironmentVariable("Jwt__RefreshTokenDays", "7");
        Environment.SetEnvironmentVariable("CORS__AllowedOrigins", "http://localhost:3000");
        Environment.SetEnvironmentVariable("SEED_ON_STARTUP", "true");
        Environment.SetEnvironmentVariable(
            "RateLimit__AuthPermitPerWindow", AuthPermitPerWindow.ToString());
        Environment.SetEnvironmentVariable("RateLimit__AuthWindowSeconds", "60");
        Environment.SetEnvironmentVariable(
            "RateLimit__LoginMaxFailures", LoginMaxFailures.ToString());
        Environment.SetEnvironmentVariable(
            "RateLimit__AuthAccountPermitPerWindow", AuthAccountPermitPerWindow.ToString());
        Environment.SetEnvironmentVariable("RateLimit__AuthAccountWindowSeconds", "900");

        // Force the host to build now, so migrations and seeding finish before any test runs.
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _container.DisposeAsync();
    }

    /// <summary>
    /// Runs work against a scoped DbContext and disposes the scope afterwards.
    ///
    /// Handing back a context from a scope nobody holds would leave it liable to be
    /// disposed under the caller, which fails intermittently and looks like a flaky test
    /// rather than a lifetime bug.
    /// </summary>
    public async Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> work)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await work(db);
    }

    public async Task WithDbAsync(Func<AppDbContext, Task> work)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await work(db);
    }
}

/// <summary>Same application, but with the auth rate limit turned down far enough that a
/// test can trip it in a handful of requests.</summary>
public class RateLimitedApiFactory : ApiFactory
{
    public const int Permitted = 3;

    protected override int AuthPermitPerWindow => Permitted;
}

/// <summary>Same application, but with the per-account throttle turned down so a test can
/// trip the account lockout without hammering the per-address limiter.</summary>
public class ThrottledApiFactory : ApiFactory
{
    public const int FailuresBeforeLockout = 3;

    protected override int LoginMaxFailures => FailuresBeforeLockout;

    protected override int AuthAccountPermitPerWindow => 100;
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
