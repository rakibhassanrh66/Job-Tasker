// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using System.Net;
using System.Net.Http.Json;
using AssignmentSystem.Application.Classes;
using AssignmentSystem.Application.Enrollments;
using AssignmentSystem.Application.Users.Dtos;
using AssignmentSystem.Domain.Enums;
using AssignmentSystem.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AssignmentSystem.IntegrationTests.Admin;

/// <summary>
/// Business rule 12 for the administrative surface: a valid token for the wrong role is
/// 403, no token at all is 401.
///
/// Every admin route is listed rather than a representative sample, because the failure
/// this guards against is one endpoint being added without a role attribute — and a
/// sample would pass while that endpoint sat wide open. M7 extends the same idea to the
/// full role-by-endpoint matrix.
/// </summary>
[Collection(ApiCollection.Name)]
public class AdminRoleGateTests
{
    private readonly ApiFactory _factory;

    public AdminRoleGateTests(ApiFactory factory) => _factory = factory;

    public static TheoryData<string, string> AdminReadRoutes => new()
    {
        { "GET", "/api/v1/users" },
        { "GET", "/api/v1/classes" },
        { "GET", "/api/v1/subjects" },
        { "GET", "/api/v1/teacher-assignments" },
        { "GET", "/api/v1/enrollments" },
        { "GET", "/api/v1/assignments" },
        { "GET", "/api/v1/submissions" }
    };

    [Theory]
    [MemberData(nameof(AdminReadRoutes))]
    public async Task Teacher_Hitting_Admin_Route_Returns_403(string method, string route)
    {
        var teacher = await _factory.AsTeacherAsync();

        var response = await teacher.SendAsync(
            new HttpRequestMessage(new HttpMethod(method), route));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            $"a Teacher token must not reach {method} {route}");
    }

    [Theory]
    [MemberData(nameof(AdminReadRoutes))]
    public async Task Student_Hitting_Admin_Route_Returns_403(string method, string route)
    {
        var student = await _factory.AsStudentAsync();

        var response = await student.SendAsync(
            new HttpRequestMessage(new HttpMethod(method), route));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            $"a Student token must not reach {method} {route}");
    }

    [Theory]
    [MemberData(nameof(AdminReadRoutes))]
    public async Task Unauthenticated_Hitting_Admin_Route_Returns_401(string method, string route)
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.SendAsync(
            new HttpRequestMessage(new HttpMethod(method), route));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "no token is 401, distinct from the 403 a wrong-role token gets");
    }

    // ---------------------------------------------------------------------------------
    // Writes are gated too, not just reads
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task Teacher_Cannot_Create_A_User()
    {
        var teacher = await _factory.AsTeacherAsync();

        var response = await teacher.PostAsJsonAsync("/api/v1/users", new CreateUserRequest(
            "Should Not Exist", $"nope-{Guid.NewGuid():N}@demo.test", "Created@123", UserRole.Admin));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "otherwise any teacher could grant themselves an administrator account");
    }

    [Fact]
    public async Task Student_Cannot_Enrol_Themselves_In_A_Class()
    {
        // The decisive one for rule 2: enrolment is what scopes visibility, so a student
        // who could enrol themselves could read any class in the system.
        var student = await _factory.AsStudentAsync();

        var response = await student.PostAsJsonAsync("/api/v1/enrollments",
            new CreateEnrollmentRequest(Guid.NewGuid(), Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Teacher_Cannot_Allocate_Themselves_To_A_Subject()
    {
        // The decisive one for rule 3: this table grants the right to create assignments,
        // so a teacher who could write to it could grant themselves that right anywhere.
        var teacher = await _factory.AsTeacherAsync();

        var response = await teacher.PostAsJsonAsync("/api/v1/teacher-assignments",
            new Application.TeacherAssignments.CreateTeacherAssignmentRequest(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Student_Cannot_Delete_A_Class()
    {
        var student = await _factory.AsStudentAsync();

        var response = await student.DeleteAsync($"/api/v1/classes/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "the role gate must reject before the handler ever looks the class up");
    }

    [Fact]
    public async Task Teacher_Cannot_Create_A_Class()
    {
        var teacher = await _factory.AsTeacherAsync();

        var response = await teacher.PostAsJsonAsync("/api/v1/classes",
            new CreateClassCourseRequest("Unauthorised Class", "UNAUTH-1"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
