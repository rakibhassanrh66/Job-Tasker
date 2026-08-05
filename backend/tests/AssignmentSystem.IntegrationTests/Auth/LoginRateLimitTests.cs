// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using System.Net;
using System.Net.Http.Json;
using AssignmentSystem.Application.Auth.Dtos;
using AssignmentSystem.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AssignmentSystem.IntegrationTests.Auth;

/// <summary>
/// Runs against its own host with the auth limit turned down, because the shared fixture
/// deliberately runs with a high limit so ordinary tests can log in freely.
///
/// Each factory sets its configuration before building its host, so the two do not
/// interfere despite configuration being process-wide — which is also why parallel
/// execution is disabled for this assembly.
/// </summary>
public class LoginRateLimitTests : IAsyncLifetime
{
    private readonly RateLimitedApiFactory _factory = new();

    public Task InitializeAsync() => _factory.InitializeAsync();

    public Task DisposeAsync() => _factory.DisposeAsync();

    [Fact]
    public async Task Login_Rate_Limit_Returns_429()
    {
        var client = _factory.CreateClient();

        // Wrong password on purpose: this is the shape of a credential-stuffing attempt,
        // and it must be the attempt that is throttled, not merely the success.
        var request = new LoginRequest(ApiClientExtensions.AdminEmail, "Wrong@123");

        var statuses = new List<HttpStatusCode>();

        for (var attempt = 0; attempt < RateLimitedApiFactory.Permitted + 3; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", request);
            statuses.Add(response.StatusCode);
        }

        statuses.Should().Contain(HttpStatusCode.TooManyRequests,
            "repeated credential attempts from one address must eventually be refused");

        statuses.Take(RateLimitedApiFactory.Permitted)
            .Should().AllSatisfy(s => s.Should().Be(HttpStatusCode.Unauthorized),
                "attempts within the limit are answered normally, just unsuccessfully");
    }

    [Fact]
    public async Task Rate_Limit_Does_Not_Apply_To_Ordinary_Endpoints()
    {
        var client = _factory.CreateClient();

        for (var i = 0; i < RateLimitedApiFactory.Permitted + 5; i++)
        {
            var response = await client.GetAsync("/api/v1/meta");
            response.StatusCode.Should().Be(HttpStatusCode.OK,
                "the limiter is scoped to the credential endpoints, not the whole API");
        }
    }
}
