// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using System.Net;
using System.Text;
using AssignmentSystem.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AssignmentSystem.IntegrationTests.Security;

/// <summary>
/// Every route, every role, in one table.
///
/// The per-module suites prove the business rules and check authorization incidentally,
/// along whichever paths those rules happen to touch. This proves the gate itself, for the
/// whole surface at once — which is the only way a route added without a role attribute
/// gets noticed. A new endpoint that is not in this table is the thing to look for when
/// reviewing: the table is meant to be exhaustive.
///
/// Deliberately asserts the gate and nothing else. For a role that is allowed through, the
/// assertion is "not 401 and not 403" rather than a specific status — whether the request
/// then 404s on a nonexistent id or 400s on an empty body is the business layer's concern
/// and is covered elsewhere. Pinning exact statuses here would make this file fail every
/// time an unrelated rule changed.
/// </summary>
[Collection(ApiCollection.Name)]
public class AuthorizationMatrixTests
{
    /// <summary>Reachable by anyone, with no token at all.</summary>
    private const string Anonymous = "anonymous";

    /// <summary>Reachable by any authenticated caller, whatever their role.</summary>
    private const string AnyRole = "*";

    private static readonly Guid MissingId = Guid.Parse("00000000-dead-beef-0000-000000000001");

    private readonly ApiFactory _factory;

    public AuthorizationMatrixTests(ApiFactory factory) => _factory = factory;

    [Theory]
    // ---- Auth and meta -------------------------------------------------------------
    [InlineData("POST", "/api/v1/auth/login", Anonymous)]
    [InlineData("POST", "/api/v1/auth/refresh", Anonymous)]
    [InlineData("GET", "/api/v1/meta", Anonymous)]
    [InlineData("GET", "/api/v1/auth/me", AnyRole)]

    // ---- Users (admin) -------------------------------------------------------------
    [InlineData("GET", "/api/v1/users", "Admin")]
    [InlineData("POST", "/api/v1/users", "Admin")]
    [InlineData("GET", "/api/v1/users/{id}", "Admin")]
    [InlineData("PUT", "/api/v1/users/{id}", "Admin")]
    [InlineData("DELETE", "/api/v1/users/{id}", "Admin")]

    // ---- Classes (admin) -----------------------------------------------------------
    [InlineData("GET", "/api/v1/classes", "Admin")]
    [InlineData("POST", "/api/v1/classes", "Admin")]
    [InlineData("GET", "/api/v1/classes/{id}", "Admin")]
    [InlineData("PUT", "/api/v1/classes/{id}", "Admin")]
    [InlineData("DELETE", "/api/v1/classes/{id}", "Admin")]

    // ---- Subjects (admin) ----------------------------------------------------------
    [InlineData("GET", "/api/v1/subjects", "Admin")]
    [InlineData("POST", "/api/v1/subjects", "Admin")]
    [InlineData("GET", "/api/v1/subjects/{id}", "Admin")]
    [InlineData("PUT", "/api/v1/subjects/{id}", "Admin")]
    [InlineData("DELETE", "/api/v1/subjects/{id}", "Admin")]

    // ---- Teacher allocations (admin) -----------------------------------------------
    // Admin-only on purpose: this table decides where a teacher may create assignments,
    // so a teacher who could edit it could grant themselves that permission.
    [InlineData("GET", "/api/v1/teacher-assignments", "Admin")]
    [InlineData("POST", "/api/v1/teacher-assignments", "Admin")]
    [InlineData("DELETE", "/api/v1/teacher-assignments/{id}", "Admin")]

    // ---- Enrolments (admin) --------------------------------------------------------
    [InlineData("GET", "/api/v1/enrollments", "Admin")]
    [InlineData("POST", "/api/v1/enrollments", "Admin")]
    [InlineData("DELETE", "/api/v1/enrollments/{id}", "Admin")]

    // ---- Assignments ---------------------------------------------------------------
    [InlineData("GET", "/api/v1/assignments", "Admin")]
    [InlineData("GET", "/api/v1/assignments/mine", "Teacher")]
    [InlineData("POST", "/api/v1/assignments", "Teacher")]
    [InlineData("PUT", "/api/v1/assignments/{id}", "Teacher")]
    [InlineData("DELETE", "/api/v1/assignments/{id}", "Teacher")]
    [InlineData("POST", "/api/v1/assignments/{id}/publish", "Teacher")]
    [InlineData("GET", "/api/v1/assignments/{id}/submissions", "Teacher")]
    [InlineData("GET", "/api/v1/assignments/available", "Student")]
    [InlineData("POST", "/api/v1/assignments/{id}/submit", "Student")]
    [InlineData("GET", "/api/v1/assignments/{id}", AnyRole)]

    // ---- Submissions ---------------------------------------------------------------
    [InlineData("GET", "/api/v1/submissions", "Admin")]
    [InlineData("PUT", "/api/v1/submissions/{id}/grade", "Teacher")]
    [InlineData("PUT", "/api/v1/submissions/{id}/status", "Teacher")]
    [InlineData("GET", "/api/v1/submissions/mine", "Student")]
    [InlineData("PUT", "/api/v1/submissions/{id}", "Student")]
    [InlineData("GET", "/api/v1/submissions/{id}", AnyRole)]
    public async Task Endpoint_Enforces_Its_Role_Gate(string method, string route, string allowed)
    {
        var path = route.Replace("{id}", MissingId.ToString());

        await AssertUnauthenticatedAsync(method, path, allowed);

        foreach (var role in new[] { "Admin", "Teacher", "Student" })
        {
            await AssertRoleAsync(method, path, role, IsAllowed(role, allowed));
        }
    }

    private static bool IsAllowed(string role, string allowed) =>
        allowed is Anonymous or AnyRole
        || allowed.Split(',').Any(r => r.Trim().Equals(role, StringComparison.OrdinalIgnoreCase));

    private async Task AssertUnauthenticatedAsync(string method, string path, string allowed)
    {
        var response = await _factory.CreateClient().SendAsync(Request(method, path));

        if (allowed == Anonymous)
        {
            response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
                $"{method} {path} is meant to be reachable without a token");

            return;
        }

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            $"{method} {path} must refuse a caller with no token");
    }

    private async Task AssertRoleAsync(string method, string path, string role, bool allowed)
    {
        var client = await ClientForAsync(role);
        var response = await client.SendAsync(Request(method, path));

        if (allowed)
        {
            response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
                $"{role} must be allowed through the role gate on {method} {path}");

            response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
                $"{role} presented a valid token to {method} {path}");

            return;
        }

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            $"{role} must be refused by the role gate on {method} {path}");
    }

    private Task<HttpClient> ClientForAsync(string role) => role switch
    {
        "Admin" => _factory.AsAdminAsync(),
        "Teacher" => _factory.AsTeacherAsync(),
        "Student" => _factory.AsStudentAsync(),
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown role.")
    };

    /// <summary>
    /// A fresh message per send — HttpRequestMessage cannot be reused. Writes and the two
    /// anonymous auth endpoints carry an empty JSON body so the request reaches the role
    /// gate on its own merits rather than being turned away at 415 for a missing content
    /// type, which would pass the assertion for the wrong reason.
    /// </summary>
    private static HttpRequestMessage Request(string method, string path) =>
        new(new HttpMethod(method), path)
        {
            Content = method is "POST" or "PUT"
                ? new StringContent("{}", Encoding.UTF8, "application/json")
                : null
        };
}
