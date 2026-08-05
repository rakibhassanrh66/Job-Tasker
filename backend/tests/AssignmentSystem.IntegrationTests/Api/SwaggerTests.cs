// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using System.Net;
using System.Text.Json;
using AssignmentSystem.Infrastructure;
using AssignmentSystem.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AssignmentSystem.IntegrationTests.Api;

/// <summary>
/// Swagger being reachable is an explicit submission requirement, so it is asserted rather
/// than assumed — a broken XML path or a mis-registered security scheme would otherwise
/// only show up when the evaluator opens the page.
/// </summary>
[Collection(ApiCollection.Name)]
public class SwaggerTests
{
    private readonly ApiFactory _factory;

    public SwaggerTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Swagger_Document_Is_Served()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Swagger_Document_Names_The_Author_As_Contact()
    {
        var client = _factory.CreateClient();

        var document = await client.GetFromJsonAsyncCompat("/swagger/v1/swagger.json");

        var contact = document.GetProperty("info").GetProperty("contact");

        contact.GetProperty("name").GetString().Should().Be(BuildInfo.Author);
        contact.GetProperty("email").GetString().Should().Be(BuildInfo.Contact);
    }

    [Fact]
    public async Task Swagger_Document_Declares_Bearer_Authentication()
    {
        var client = _factory.CreateClient();

        var document = await client.GetFromJsonAsyncCompat("/swagger/v1/swagger.json");

        var schemes = document.GetProperty("components").GetProperty("securitySchemes");

        schemes.TryGetProperty("Bearer", out var bearer).Should().BeTrue(
            "the docs must let an evaluator paste a token and try the protected routes");

        bearer.GetProperty("scheme").GetString().Should().Be("bearer");
    }

    [Fact]
    public async Task Swagger_Documents_Both_Shapes_Of_GetAssignmentById()
    {
        var client = _factory.CreateClient();

        var document = await client.GetFromJsonAsyncCompat("/swagger/v1/swagger.json");

        var schema = document
            .GetProperty("paths")
            .GetProperty("/api/v1/assignments/{id}")
            .GetProperty("get")
            .GetProperty("responses")
            .GetProperty("200")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema");

        schema.TryGetProperty("oneOf", out var oneOf).Should().BeTrue(
            "the route answers in a different shape per role, and documenting only one "
            + "of them would misdescribe the contract for the other");

        var referenced = oneOf.EnumerateArray()
            .Select(s => s.GetProperty("$ref").GetString()!.Split('/')[^1])
            .ToArray();

        referenced.Should().BeEquivalentTo("StudentAssignmentDto", "AssignmentDto");
    }

    [Fact]
    public async Task Swagger_Documents_The_400_On_Guarded_List_Endpoints()
    {
        var client = _factory.CreateClient();

        var document = await client.GetFromJsonAsyncCompat("/swagger/v1/swagger.json");

        var responses = document
            .GetProperty("paths")
            .GetProperty("/api/v1/assignments/available")
            .GetProperty("get")
            .GetProperty("responses");

        responses.TryGetProperty("400", out _).Should().BeTrue(
            "an unrecognised filter is rejected rather than ignored, so the contract "
            + "has to say so");
    }

    [Fact]
    public async Task Swagger_Does_Not_Offer_Students_Filters_They_Cannot_Use()
    {
        var client = _factory.CreateClient();

        var document = await client.GetFromJsonAsyncCompat("/swagger/v1/swagger.json");

        var parameters = document
            .GetProperty("paths")
            .GetProperty("/api/v1/assignments/available")
            .GetProperty("get")
            .GetProperty("parameters")
            .EnumerateArray()
            .Select(p => p.GetProperty("name").GetString())
            .ToArray();

        parameters.Should().NotContain("Status",
            "a student's list is always Published, so advertising a status filter would "
            + "promise something the endpoint cannot do");

        parameters.Should().Contain("TeacherId");
    }

    [Fact]
    public async Task Api_Responses_Carry_The_Hardening_Headers()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/meta");

        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
        response.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");
        response.Headers.GetValues("Referrer-Policy").Should().Contain("no-referrer");

        response.Headers.GetValues("Content-Security-Policy").Single()
            .Should().Be("default-src 'none'; frame-ancestors 'none'",
                "a JSON response needs to load nothing, so the strictest policy applies");
    }

    [Fact]
    public async Task Swagger_Ui_Gets_A_Policy_That_Lets_It_Actually_Render()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/index.html");

        var policy = response.Headers.GetValues("Content-Security-Policy").Single();

        // Swashbuckle emits an inline bootstrap script. Under the API's default-src 'none'
        // the docs page would load and then show nothing at all.
        policy.Should().Contain("script-src 'self' 'unsafe-inline'");
        policy.Should().Contain("frame-ancestors 'none'");
    }

    [Fact]
    public async Task Swagger_Document_Includes_The_Auth_And_Meta_Routes()
    {
        var client = _factory.CreateClient();

        var document = await client.GetFromJsonAsyncCompat("/swagger/v1/swagger.json");

        var paths = document.GetProperty("paths");

        paths.TryGetProperty("/api/v1/auth/login", out _).Should().BeTrue();
        paths.TryGetProperty("/api/v1/auth/refresh", out _).Should().BeTrue();
        paths.TryGetProperty("/api/v1/auth/me", out _).Should().BeTrue();
        paths.TryGetProperty("/api/v1/meta", out _).Should().BeTrue();
    }
}

internal static class HttpClientJsonHelpers
{
    public static async Task<JsonElement> GetFromJsonAsyncCompat(
        this HttpClient client, string requestUri)
    {
        var raw = await client.GetStringAsync(requestUri);
        using var document = JsonDocument.Parse(raw);

        // Cloned so the element stays usable after the JsonDocument is disposed.
        return document.RootElement.Clone();
    }
}
