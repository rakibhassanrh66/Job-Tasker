// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using System.Net;
using System.Net.Http.Json;
using AssignmentSystem.Application.Classes;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Application.Enrollments;
using AssignmentSystem.Application.Subjects;
using AssignmentSystem.Application.TeacherAssignments;
using AssignmentSystem.Application.Users.Dtos;
using AssignmentSystem.Domain.Enums;
using AssignmentSystem.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AssignmentSystem.IntegrationTests.Admin;

[Collection(ApiCollection.Name)]
public class AdminModuleTests
{
    private readonly ApiFactory _factory;

    public AdminModuleTests(ApiFactory factory) => _factory = factory;

    /// <summary>Unique per call so tests can run repeatedly against a shared database.</summary>
    private static string UniqueEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@demo.test";

    // ---------------------------------------------------------------------------------
    // Users
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task Admin_Can_Create_Teacher_And_Student()
    {
        var admin = await _factory.AsAdminAsync();

        foreach (var role in new[] { UserRole.Teacher, UserRole.Student })
        {
            var response = await admin.PostAsJsonAsync("/api/v1/users", new CreateUserRequest(
                FullName: $"Created {role}",
                Email: UniqueEmail(role.ToString().ToLower()),
                Password: "Created@123",
                Role: role));

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var created = await response.Content.ReadFromJsonAsync<UserDto>();
            created!.Role.Should().Be(role);
            created.IsActive.Should().BeTrue();
        }
    }

    [Fact]
    public async Task Created_User_Can_Immediately_Log_In()
    {
        var admin = await _factory.AsAdminAsync();
        var email = UniqueEmail("loginable");

        await admin.PostAsJsonAsync("/api/v1/users", new CreateUserRequest(
            "Loginable Person", email, "Created@123", UserRole.Student));

        var anonymous = _factory.CreateClient();

        var login = await anonymous.PostAsJsonAsync(
            "/api/v1/auth/login", new Application.Auth.Dtos.LoginRequest(email, "Created@123"));

        login.StatusCode.Should().Be(HttpStatusCode.OK,
            "the password set at creation must be the password that works at login");
    }

    [Fact]
    public async Task Cannot_Create_User_With_Duplicate_Email_Returns_409()
    {
        var admin = await _factory.AsAdminAsync();
        var email = UniqueEmail("duplicate");

        var first = await admin.PostAsJsonAsync("/api/v1/users", new CreateUserRequest(
            "First Person", email, "Created@123", UserRole.Student));

        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await admin.PostAsJsonAsync("/api/v1/users", new CreateUserRequest(
            "Second Person", email, "Created@123", UserRole.Student));

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Duplicate_Email_Detection_Ignores_Casing()
    {
        var admin = await _factory.AsAdminAsync();
        var email = UniqueEmail("casing");

        await admin.PostAsJsonAsync("/api/v1/users", new CreateUserRequest(
            "Lower Case", email, "Created@123", UserRole.Student));

        var upper = await admin.PostAsJsonAsync("/api/v1/users", new CreateUserRequest(
            "Upper Case", email.ToUpperInvariant(), "Created@123", UserRole.Student));

        upper.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "emails are normalised, so differing case is still the same account");
    }

    [Fact]
    public async Task Weak_Password_Is_Rejected_With_422()
    {
        var admin = await _factory.AsAdminAsync();

        var response = await admin.PostAsJsonAsync("/api/v1/users", new CreateUserRequest(
            "Weak Password", UniqueEmail("weak"), "short", UserRole.Student));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Deactivated_User_Cannot_Login()
    {
        var admin = await _factory.AsAdminAsync();
        var email = UniqueEmail("deactivated");

        var created = await (await admin.PostAsJsonAsync("/api/v1/users", new CreateUserRequest(
            "Soon Deactivated", email, "Created@123", UserRole.Student)))
            .Content.ReadFromJsonAsync<UserDto>();

        var anonymous = _factory.CreateClient();

        var beforeDeactivation = await anonymous.PostAsJsonAsync(
            "/api/v1/auth/login", new Application.Auth.Dtos.LoginRequest(email, "Created@123"));

        beforeDeactivation.StatusCode.Should().Be(HttpStatusCode.OK);

        var delete = await admin.DeleteAsync($"/api/v1/users/{created!.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterDeactivation = await anonymous.PostAsJsonAsync(
            "/api/v1/auth/login", new Application.Auth.Dtos.LoginRequest(email, "Created@123"));

        afterDeactivation.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Deactivating_A_User_Keeps_The_Record_Rather_Than_Deleting_It()
    {
        var admin = await _factory.AsAdminAsync();
        var email = UniqueEmail("preserved");

        var created = await (await admin.PostAsJsonAsync("/api/v1/users", new CreateUserRequest(
            "Preserved Person", email, "Created@123", UserRole.Student)))
            .Content.ReadFromJsonAsync<UserDto>();

        await admin.DeleteAsync($"/api/v1/users/{created!.Id}");

        var stillThere = await _factory.WithDbAsync(db =>
            db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == created.Id));

        stillThere.Should().NotBeNull("authored assignments and graded submissions reference this row");
        stillThere!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Admin_Cannot_Deactivate_Their_Own_Account()
    {
        var admin = await _factory.AsAdminAsync();

        var me = await admin.GetFromJsonAsync<Application.Auth.Dtos.UserProfile>("/api/v1/auth/me");

        var response = await admin.DeleteAsync($"/api/v1/users/{me!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "an administrator disabling themselves could lock everyone out of the system");
    }

    // ---------------------------------------------------------------------------------
    // Classes and subjects
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task Cannot_Delete_Class_With_Enrollments_Returns_409()
    {
        var admin = await _factory.AsAdminAsync();

        // CS-101 is seeded with enrolled students.
        var classes = await admin.GetFromJsonAsync<PagedResult<ClassCourseDto>>(
            "/api/v1/classes?search=CS-101");

        var cs101 = classes!.Items.Single(c => c.Code == "CS-101");
        cs101.EnrollmentCount.Should().BeGreaterThan(0);

        var response = await admin.DeleteAsync($"/api/v1/classes/{cs101.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Empty_Class_Can_Be_Deleted()
    {
        var admin = await _factory.AsAdminAsync();

        var created = await (await admin.PostAsJsonAsync("/api/v1/classes",
                new CreateClassCourseRequest("Disposable Class", $"TMP-{Guid.NewGuid():N}"[..10])))
            .Content.ReadFromJsonAsync<ClassCourseDto>();

        var response = await admin.DeleteAsync($"/api/v1/classes/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "the guard is about dependants, not about refusing deletion outright");
    }

    [Fact]
    public async Task Duplicate_Class_Code_Returns_409()
    {
        var admin = await _factory.AsAdminAsync();

        var response = await admin.PostAsJsonAsync("/api/v1/classes",
            new CreateClassCourseRequest("Clashing Class", "CS-101"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Subject_Must_Belong_To_Existing_Class()
    {
        var admin = await _factory.AsAdminAsync();

        var response = await admin.PostAsJsonAsync("/api/v1/subjects",
            new CreateSubjectRequest("Orphan Subject", "ORP-1", Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---------------------------------------------------------------------------------
    // Enrolments and teacher allocations
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task Duplicate_Enrollment_Returns_409()
    {
        var admin = await _factory.AsAdminAsync();

        var existing = await _factory.WithDbAsync(db =>
            db.Enrollments.AsNoTracking().FirstAsync());

        var response = await admin.PostAsJsonAsync("/api/v1/enrollments",
            new CreateEnrollmentRequest(existing.StudentId, existing.ClassCourseId));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Enrolling_A_Teacher_As_A_Student_Returns_422()
    {
        var admin = await _factory.AsAdminAsync();

        var teacher = await _factory.WithDbAsync(db =>
            db.Users.AsNoTracking().FirstAsync(u => u.Role == UserRole.Teacher));

        var classCourse = await _factory.WithDbAsync(db =>
            db.ClassCourses.AsNoTracking().FirstAsync());

        var response = await admin.PostAsJsonAsync("/api/v1/enrollments",
            new CreateEnrollmentRequest(teacher.Id, classCourse.Id));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Duplicate_TeacherAssignment_Returns_409()
    {
        var admin = await _factory.AsAdminAsync();

        var existing = await _factory.WithDbAsync(db =>
            db.TeacherAssignments.AsNoTracking().FirstAsync());

        var response = await admin.PostAsJsonAsync("/api/v1/teacher-assignments",
            new CreateTeacherAssignmentRequest(
                existing.TeacherId, existing.SubjectId, existing.ClassCourseId));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Allocating_A_Subject_To_The_Wrong_Class_Returns_422()
    {
        // The subject belongs to one class; pairing it with another would create an
        // allocation that rule 3 could match for a class/subject pair that does not exist.
        var admin = await _factory.AsAdminAsync();

        var teacher = await _factory.WithDbAsync(db =>
            db.Users.AsNoTracking().FirstAsync(u => u.Role == UserRole.Teacher));

        var subject = await _factory.WithDbAsync(db =>
            db.Subjects.AsNoTracking().FirstAsync());

        var otherClass = await _factory.WithDbAsync(db =>
            db.ClassCourses.AsNoTracking().FirstAsync(c => c.Id != subject.ClassCourseId));

        var response = await admin.PostAsJsonAsync("/api/v1/teacher-assignments",
            new CreateTeacherAssignmentRequest(teacher.Id, subject.Id, otherClass.Id));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ---------------------------------------------------------------------------------
    // Oversight
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task Admin_Can_Read_All_Assignments_And_Submissions()
    {
        var admin = await _factory.AsAdminAsync();

        var assignments = await admin.GetFromJsonAsync<PagedResult<
            Application.Assignments.Dtos.AssignmentDto>>("/api/v1/assignments");

        var submissions = await admin.GetFromJsonAsync<PagedResult<
            Application.Submissions.Dtos.SubmissionDto>>("/api/v1/submissions");

        assignments!.TotalCount.Should().BeGreaterThan(0);
        submissions!.TotalCount.Should().BeGreaterThan(0);

        // Oversight means everything, including drafts that students must never see.
        assignments.Items.Should().Contain(a => a.Status == AssignmentStatus.Draft);
    }
}
