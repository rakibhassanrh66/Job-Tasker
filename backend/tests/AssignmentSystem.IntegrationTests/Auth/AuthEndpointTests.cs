// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AssignmentSystem.Application.Auth.Dtos;
using AssignmentSystem.Infrastructure;
using AssignmentSystem.Infrastructure.Auth;
using AssignmentSystem.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace AssignmentSystem.IntegrationTests.Auth;

[Collection(ApiCollection.Name)]
public class AuthEndpointTests
{
    private readonly ApiFactory _factory;

    public AuthEndpointTests(ApiFactory factory) => _factory = factory;

    // ---------------------------------------------------------------------------------
    // Business rule 12 — unauthenticated is 401
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task Unauthenticated_Request_Returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Expired_Token_Returns_401()
    {
        // Signed with the correct key, so this proves lifetime is actually validated
        // rather than the request failing for some unrelated reason.
        var expired = CreateTokenExpiredAt(DateTime.UtcNow.AddMinutes(-5));

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expired);

        var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_Signed_With_A_Different_Key_Returns_401()
    {
        var forged = CreateTokenExpiredAt(
            DateTime.UtcNow.AddHours(1),
            signingKey: "a-completely-different-key-that-is-also-long-enough-32");

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", forged);

        var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "a token this API did not sign must never be accepted");
    }

    // ---------------------------------------------------------------------------------
    // Login and the authenticated round trip
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task Login_Then_Call_Me_Returns_Profile()
    {
        var client = await _factory.AsTeacherAsync();

        var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.Content.ReadFromJsonAsync<UserProfile>();

        profile!.Email.Should().Be(ApiClientExtensions.TeacherEmail);
        profile.Role.Should().Be(Domain.Enums.UserRole.Teacher);
    }

    [Fact]
    public async Task Me_Response_Never_Contains_A_Password_Hash()
    {
        var client = await _factory.AsStudentAsync();

        var raw = await client.GetStringAsync("/api/v1/auth/me");

        raw.Should().NotContain("passwordHash", "DTOs are projected, entities are not returned");
        raw.Should().NotContain("AQAAAAIAAYag", "no hash prefix may appear in a response body");
    }

    [Fact]
    public async Task Login_With_Wrong_Password_Returns_401_As_ProblemDetails()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(ApiClientExtensions.AdminEmail, "NotThePassword@1"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.Should().Be("Authentication failed");
        problem.Status.Should().Be(401);
    }

    [Fact]
    public async Task Login_With_Invalid_Request_Returns_422_With_Field_Errors()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginRequest("not-an-email", ""));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "the request was understood but broke a validation rule — 422, not 400");

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKey(nameof(LoginRequest.Email));
        problem.Errors.Should().ContainKey(nameof(LoginRequest.Password));
    }

    [Fact]
    public async Task Malformed_Json_Returns_400_Not_422()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync(
            "/api/v1/auth/login",
            new StringContent("{ this is not json", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "a request that cannot be parsed is malformed, not merely invalid");
    }

    // ---------------------------------------------------------------------------------
    // Refresh over HTTP
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task Refresh_Returns_A_New_Token_Pair_And_Retires_The_Old_One()
    {
        var client = _factory.CreateClient();

        var login = await client.LoginAsync(
            ApiClientExtensions.StudentEmail, ApiClientExtensions.StudentPassword);

        var refreshResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh", new RefreshRequest(login.RefreshToken));

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();
        refreshed!.RefreshToken.Should().NotBe(login.RefreshToken);

        // The old token is spent.
        var replay = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh", new RefreshRequest(login.RefreshToken));

        replay.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------------------------------------------------------------------------------
    // Provenance surface
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task Meta_Endpoint_Is_Anonymous_And_Returns_Canary()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/meta");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        body.GetProperty("canary").GetString().Should().Be(BuildInfo.Canary);
        body.GetProperty("author").GetString().Should().Be(BuildInfo.Author);
    }

    [Fact]
    public async Task Every_Response_Carries_The_Authorship_Headers()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/meta");

        response.Headers.GetValues("X-Built-By").Should().ContainSingle()
            .Which.Should().Contain(BuildInfo.Author);

        response.Headers.GetValues("X-Canary").Should().ContainSingle()
            .Which.Should().Be(BuildInfo.Canary);
    }

    // ---------------------------------------------------------------------------------
    // Error shape
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task Error_Response_Is_ProblemDetails_With_TraceId()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(ApiClientExtensions.AdminEmail, "NotThePassword@1"));

        response.Content.Headers.ContentType!.MediaType
            .Should().Be("application/problem+json");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        body.TryGetProperty("traceId", out var traceId).Should().BeTrue(
            "a traceId is what connects a user's error report to a log line");
        traceId.GetString().Should().NotBeNullOrWhiteSpace();

        body.GetProperty("title").GetString().Should().Be("Authentication failed");
        body.GetProperty("status").GetInt32().Should().Be(401);
    }

    [Fact]
    public async Task Error_Response_Does_Not_Leak_A_Stack_Trace_Or_Connection_String()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(ApiClientExtensions.AdminEmail, "NotThePassword@1"));

        var raw = await response.Content.ReadAsStringAsync();

        raw.Should().NotContain("at AssignmentSystem.");
        raw.Should().NotContain("Password=");
        raw.Should().NotContain("Host=");
    }

    // ---------------------------------------------------------------------------------

    private static string CreateTokenExpiredAt(DateTime expires, string? signingKey = null)
    {
        var key = signingKey ?? "integration-test-signing-key-long-enough-for-hmac-sha256";

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "AssignmentSystem.Api",
            audience: "AssignmentSystem.Client",
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
                new Claim(JwtTokenService.RoleClaimType, "Student")
            },
            notBefore: expires.AddMinutes(-30),
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
