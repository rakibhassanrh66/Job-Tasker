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
