// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using System.Net;
using System.Net.Http.Json;
using AssignmentSystem.Application.Assignments.Dtos;
using AssignmentSystem.Application.Classes;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Application.Enrollments;
using AssignmentSystem.Application.Subjects;
using AssignmentSystem.Application.Submissions.Dtos;
using AssignmentSystem.Application.TeacherAssignments;
using AssignmentSystem.Application.Users.Dtos;
using AssignmentSystem.Domain.Enums;
using AssignmentSystem.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AssignmentSystem.IntegrationTests.Workflow;

/// <summary>
/// The whole product, once, over real HTTP — from an empty class to a graded submission the
/// student can read back.
///
/// Every other suite tests one module against seeded fixtures. This one builds its own world
/// from nothing through the public API and then uses it, which is the only way the seams get
/// exercised: that the id an admin hands back is the id the teacher can allocate against,
/// that an allocation actually unlocks assignment creation, that publishing is what makes a
/// draft visible to an enrolled student, and that a mark entered by a teacher arrives intact
/// at the student who wrote the answer.
///
/// Nothing here touches the DbContext. If a step can only be done by reaching behind the API,
/// an evaluator following the README could not do it either.
/// </summary>
[Collection(ApiCollection.Name)]
public class EndToEndWorkflowTests
{
    private const string Password = "E2ePass123";

    private readonly ApiFactory _factory;

    public EndToEndWorkflowTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Admin_Provisions_Teacher_Assigns_Student_Submits_Teacher_Grades()
    {
        // Unique per run so the test never collides with the seed data or with itself.
        var tag = Guid.NewGuid().ToString("N")[..8];

        var admin = await _factory.AsAdminAsync();

        // ---- 1. Admin builds the structure ------------------------------------------

        var classCourse = await CreatedAsync<ClassCourseDto>(admin, "/api/v1/classes",
            new CreateClassCourseRequest($"End to end class {tag}", $"E2E-{tag}"));

        var subject = await CreatedAsync<SubjectDto>(admin, "/api/v1/subjects",
            new CreateSubjectRequest($"End to end subject {tag}", $"E2ES-{tag}", classCourse.Id));

        var teacher = await CreatedAsync<UserDto>(admin, "/api/v1/users",
            new CreateUserRequest($"Teacher {tag}", $"teacher.{tag}@e2e.test", Password, UserRole.Teacher));

        var student = await CreatedAsync<UserDto>(admin, "/api/v1/users",
            new CreateUserRequest($"Student {tag}", $"student.{tag}@e2e.test", Password, UserRole.Student));

        // The allocation is what grants this teacher the right to set work in this subject.
        await CreatedAsync<TeacherAssignmentDto>(admin, "/api/v1/teacher-assignments",
            new CreateTeacherAssignmentRequest(teacher.Id, subject.Id, classCourse.Id));

        await CreatedAsync<EnrollmentDto>(admin, "/api/v1/enrollments",
            new CreateEnrollmentRequest(student.Id, classCourse.Id));

        // ---- 2. The teacher sets work -----------------------------------------------

        var teacherClient = await _factory.AuthenticatedClientAsync($"teacher.{tag}@e2e.test", Password);
        var studentClient = await _factory.AuthenticatedClientAsync($"student.{tag}@e2e.test", Password);

        var assignment = await CreatedAsync<AssignmentDto>(teacherClient, "/api/v1/assignments",
            new CreateAssignmentRequest(
                Title: $"End to end assignment {tag}",
                Description: "Written by the end-to-end workflow test.",
                Deadline: DateTime.UtcNow.AddDays(7),
                MaxMarks: 50,
                ClassCourseId: classCourse.Id,
                SubjectId: subject.Id,
                AllowLateSubmission: false,
                AllowUpdateBeforeDeadline: true));

        assignment.Status.Should().Be(AssignmentStatus.Draft,
            "work is created as a draft so nothing reaches students by accident");

        // ---- 3. A draft is invisible to the enrolled student ------------------------

        var beforePublish = await AvailableAsync(studentClient);

        beforePublish.Should().NotContain(a => a.Id == assignment.Id,
            "the student is enrolled in the class, so only the draft status can be hiding it");

        // ---- 4. Publishing is what makes it visible ---------------------------------

        var publish = await teacherClient.PostAsync($"/api/v1/assignments/{assignment.Id}/publish", null);
        publish.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterPublish = await AvailableAsync(studentClient);

        afterPublish.Should().ContainSingle(a => a.Id == assignment.Id,
            "publishing is the only thing that changed between the two reads");

        // ---- 5. The student submits --------------------------------------------------

        var submitResponse = await studentClient.PostAsJsonAsync(
            $"/api/v1/assignments/{assignment.Id}/submit",
            new CreateSubmissionRequest("My end-to-end answer.", null));

        submitResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var submission = (await submitResponse.Content.ReadFromJsonAsync<SubmissionDto>())!;
        submission.Status.Should().Be(SubmissionStatus.Submitted);

        // ---- 6. The teacher finds it and grades it -----------------------------------

        var queue = await teacherClient.GetFromJsonAsync<PagedResult<SubmissionDto>>(
            $"/api/v1/assignments/{assignment.Id}/submissions");

        queue!.Items.Should().ContainSingle(s => s.Id == submission.Id,
            "the submission must reach the teacher who set the work");

        var gradeResponse = await teacherClient.PutAsJsonAsync(
            $"/api/v1/submissions/{submission.Id}/grade",
            new GradeSubmissionRequest(44, "Clear reasoning throughout."));

        gradeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // ---- 7. The student reads the mark back --------------------------------------

        var mine = await studentClient.GetFromJsonAsync<PagedResult<SubmissionDto>>(
            $"/api/v1/submissions/mine?assignmentId={assignment.Id}");

        var graded = mine!.Items.Single();

        graded.Marks.Should().Be(44);
        graded.MaxMarks.Should().Be(50, "the client renders 44 / 50 without a second request");
        graded.Feedback.Should().Be("Clear reasoning throughout.");
        graded.Status.Should().Be(SubmissionStatus.Graded);
    }

    [Fact]
    public async Task A_Teacher_Cannot_Set_Work_In_A_Subject_They_Are_Not_Allocated_To()
    {
        // The mirror of step 1 above: the same structure, minus the allocation. Proves the
        // allocation in the happy path is doing the work rather than being decoration.
        var tag = Guid.NewGuid().ToString("N")[..8];

        var admin = await _factory.AsAdminAsync();

        var classCourse = await CreatedAsync<ClassCourseDto>(admin, "/api/v1/classes",
            new CreateClassCourseRequest($"Unallocated class {tag}", $"UNA-{tag}"));

        var subject = await CreatedAsync<SubjectDto>(admin, "/api/v1/subjects",
            new CreateSubjectRequest($"Unallocated subject {tag}", $"UNAS-{tag}", classCourse.Id));

        var teacher = await CreatedAsync<UserDto>(admin, "/api/v1/users",
            new CreateUserRequest($"Teacher {tag}", $"unallocated.{tag}@e2e.test", Password, UserRole.Teacher));

        teacher.Role.Should().Be(UserRole.Teacher);

        var teacherClient = await _factory.AuthenticatedClientAsync($"unallocated.{tag}@e2e.test", Password);

        var response = await teacherClient.PostAsJsonAsync("/api/v1/assignments",
            new CreateAssignmentRequest(
                Title: $"Should be refused {tag}",
                Description: "Created without an allocation.",
                Deadline: DateTime.UtcNow.AddDays(7),
                MaxMarks: 50,
                ClassCourseId: classCourse.Id,
                SubjectId: subject.Id,
                AllowLateSubmission: false,
                AllowUpdateBeforeDeadline: true));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "being a teacher is not the same as being this subject's teacher");
    }

    // ---------------------------------------------------------------------------------

    private static async Task<T> CreatedAsync<T>(HttpClient client, string path, object body)
    {
        var response = await client.PostAsJsonAsync(path, body);

        // Surfaces the server's ProblemDetails in the failure message rather than a bare
        // "expected Created but found UnprocessableEntity", which says nothing about why.
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "POST {0} failed: {1}", path, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private static async Task<IReadOnlyList<StudentAssignmentDto>> AvailableAsync(HttpClient student)
    {
        var page = await student.GetFromJsonAsync<PagedResult<StudentAssignmentDto>>(
            "/api/v1/assignments/available?pageSize=100");

        return page!.Items;
    }
}
