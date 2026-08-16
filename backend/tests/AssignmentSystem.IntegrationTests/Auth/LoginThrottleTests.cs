// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using System.Net;
using System.Net.Http.Json;
using AssignmentSystem.Application.Auth.Dtos;
using AssignmentSystem.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AssignmentSystem.IntegrationTests.Auth;

/// <summary>
/// Exercises the per-account throttle over HTTP using its own host with the lockout
/// threshold turned down (see <see cref="ThrottledApiFactory"/>). The per-address limiter
/// and the per-account attempt window are both left high so the 429s here come from the
/// escalating failure lockout, not from one of the other guards.
/// </summary>
public class LoginThrottleTests : IAsyncLifetime
{
    private readonly ThrottledApiFactory _factory = new();

    public Task InitializeAsync() => _factory.InitializeAsync();

    public Task DisposeAsync() => _factory.DisposeAsync();

    [Fact]
    public async Task Repeated_Failed_Logins_Eventually_Return_429()
    {
        var client = _factory.CreateClient();
        var request = new LoginRequest(ApiClientExtensions.AdminEmail, "Wrong@123");

        var statuses = new List<HttpStatusCode>();

        for (var attempt = 0; attempt < ThrottledApiFactory.FailuresBeforeLockout + 2; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", request);
            statuses.Add(response.StatusCode);
        }

        // Attempts up to the threshold are ordinary wrong-password 401s.
        statuses.Take(ThrottledApiFactory.FailuresBeforeLockout)
            .Should().AllSatisfy(s => s.Should().Be(HttpStatusCode.Unauthorized));

        // The first attempt past the threshold is refused with 429.
        statuses[ThrottledApiFactory.FailuresBeforeLockout]
            .Should().Be(HttpStatusCode.TooManyRequests);

        // The lockout message carries the retry window, not a credential hint.
        var problemResponse = await client.PostAsJsonAsync("/api/v1/auth/login", request);
        var problem = await problemResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(429);
        problem.Detail.Should().Contain("Try again");
    }

    [Fact]
    public async Task A_Successful_Login_Clears_The_Failed_Attempt_State()
    {
        var client = _factory.CreateClient();

        // Two failures, then a success: the counter resets.
        for (var i = 0; i < 2; i++)
        {
            var failed = await client.PostAsJsonAsync("/api/v1/auth/login",
                new LoginRequest(ApiClientExtensions.AdminEmail, "Wrong@123"));
            failed.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var ok = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(ApiClientExtensions.AdminEmail, ApiClientExtensions.AdminPassword));
        ok.StatusCode.Should().Be(HttpStatusCode.OK);

        // Three failures after the reset must all be answered 401 (a fresh counter needs
        // three failures to lock again). Had the earlier failures been carried over, the
        // second of these would already be a 429.
        var after = new List<HttpStatusCode>();
        for (var i = 0; i < 3; i++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login",
                new LoginRequest(ApiClientExtensions.AdminEmail, "Wrong@123"));
            after.Add(response.StatusCode);
        }

        after.Should().AllSatisfy(s => s.Should().Be(HttpStatusCode.Unauthorized),
            "a successful login must reset the counter so a typo does not keep the account locked");

        // The lockout re-engages once the fresh counter reaches the threshold again.
        var locked = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(ApiClientExtensions.AdminEmail, "Wrong@123"));
        locked.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
