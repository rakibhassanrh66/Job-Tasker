// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using System.Net.Http.Headers;
using System.Net.Http.Json;
using AssignmentSystem.Application.Auth.Dtos;

namespace AssignmentSystem.IntegrationTests.Infrastructure;

/// <summary>Shared helpers so tests read as intent rather than plumbing.</summary>
public static class ApiClientExtensions
{
    public const string AdminEmail = "admin@demo.test";
    public const string TeacherEmail = "teacher@demo.test";
    public const string SecondTeacherEmail = "teacher2@demo.test";
    public const string StudentEmail = "student@demo.test";
    public const string SecondStudentEmail = "student2@demo.test";

    public const string AdminPassword = "Admin@123";
    public const string TeacherPassword = "Teacher@123";
    public const string StudentPassword = "Student@123";

    public static async Task<AuthResponse> LoginAsync(
        this HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginRequest(email, password));

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    /// <summary>Returns a client already carrying a bearer token for the given account.</summary>
    public static async Task<HttpClient> AuthenticatedClientAsync(
        this ApiFactory factory, string email, string password)
    {
        var client = factory.CreateClient();
        var auth = await client.LoginAsync(email, password);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        return client;
    }

    public static Task<HttpClient> AsAdminAsync(this ApiFactory factory) =>
        factory.AuthenticatedClientAsync(AdminEmail, AdminPassword);

    public static Task<HttpClient> AsTeacherAsync(this ApiFactory factory) =>
        factory.AuthenticatedClientAsync(TeacherEmail, TeacherPassword);

    public static Task<HttpClient> AsStudentAsync(this ApiFactory factory) =>
        factory.AuthenticatedClientAsync(StudentEmail, StudentPassword);
}
