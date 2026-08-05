// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using System.Net;
using System.Net.Http.Json;
using AssignmentSystem.Application.Assignments.Dtos;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Application.Submissions.Dtos;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Enums;
using AssignmentSystem.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AssignmentSystem.IntegrationTests.Student;

/// <summary>
/// Business rules 1, 2, 5, 6, 7 and 8 over real HTTP.
///
/// student@demo.test is enrolled in CS-101 only, and the seed data puts a published
/// assignment in MATH-201 — so "a class the student is not in" is a real case rather than
/// a hypothetical one.
/// </summary>
[Collection(ApiCollection.Name)]
public class StudentModuleTests
{
    private readonly ApiFactory _factory;

    public StudentModuleTests(ApiFactory factory) => _factory = factory;

    // ---------------------------------------------------------------------------------
    // Business rule 1 — students never see Draft or Archived
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task Student_Cannot_See_Draft_Assignments()
    {
        var student = await _factory.AsStudentAsync();

        var available = await student.GetFromJsonAsync<PagedResult<StudentAssignmentDto>>(
            "/api/v1/assignments/available?pageSize=100");

        var draftIds = await _factory.WithDbAsync(db => db.Assignments
            .Where(a => a.Status == AssignmentStatus.Draft)
            .Select(a => a.Id)
            .ToListAsync());

        draftIds.Should().NotBeEmpty("the seed includes a draft so this can be tested at all");
        available!.Items.Select(a => a.Id).Should().NotIntersectWith(draftIds);
    }

    [Fact]
    public async Task Student_Cannot_See_Archived_Assignments()
    {
        var student = await _factory.AsStudentAsync();
        var archived = await CreateAssignmentAsync(status: AssignmentStatus.Archived);

        var available = await student.GetFromJsonAsync<PagedResult<StudentAssignmentDto>>(
            "/api/v1/assignments/available?pageSize=100");

        available!.Items.Should().NotContain(a => a.Id == archived);
    }

    [Fact]
    public async Task Student_Requesting_Draft_By_Id_Returns_404()
    {
        var student = await _factory.AsStudentAsync();
        var draft = await CreateAssignmentAsync(status: AssignmentStatus.Draft);

        var response = await student.GetAsync($"/api/v1/assignments/{draft}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "a 403 would confirm that an assignment with this id is being prepared, "
            + "which is precisely what hiding drafts is meant to prevent");
    }

    // ---------------------------------------------------------------------------------
    // Business rule 2 — class scoping
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task Student_Only_Sees_Assignments_For_Enrolled_Class()
    {
        var student = await _factory.AsStudentAsync();

        var enrolledClassIds = await _factory.WithDbAsync(db => db.Enrollments
            .Where(e => e.Student.Email == ApiClientExtensions.StudentEmail)
            .Select(e => e.ClassCourseId)
            .ToListAsync());

        var available = await student.GetFromJsonAsync<PagedResult<StudentAssignmentDto>>(
            "/api/v1/assignments/available?pageSize=100");

        available!.Items.Should().NotBeEmpty();
        available.Items.Should().OnlyContain(a => enrolledClassIds.Contains(a.ClassCourseId));
    }

    [Fact]
    public async Task Student_Requesting_Other_Class_Assignment_By_Id_Returns_403()
    {
        var student = await _factory.AsStudentAsync();

        // Published, but in MATH-201, where this student is not enrolled.
        var otherClassAssignment = await _factory.WithDbAsync(db => db.Assignments
            .Where(a => a.Status == AssignmentStatus.Published
                        && a.ClassCourse.Code == "MATH-201")
            .Select(a => a.Id)
            .FirstAsync());

        var response = await student.GetAsync($"/api/v1/assignments/{otherClassAssignment}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Student_Cannot_Submit_To_Assignment_Of_Unenrolled_Class_Returns_403()
    {
        var student = await _factory.AsStudentAsync();

        var otherClassAssignment = await _factory.WithDbAsync(db => db.Assignments
            .Where(a => a.Status == AssignmentStatus.Published
                        && a.ClassCourse.Code == "MATH-201")
            .Select(a => a.Id)
            .FirstAsync());

        var response = await student.PostAsJsonAsync(
            $"/api/v1/assignments/{otherClassAssignment}/submit",
            new CreateSubmissionRequest("Trying another class's work", null));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---------------------------------------------------------------------------------
    // Business rule 5 — the deadline
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task Submit_Before_Deadline_Succeeds_With_Status_Submitted()
    {
        var student = await _factory.AsStudentAsync();
        var assignment = await CreateAssignmentAsync(deadline: DateTime.UtcNow.AddDays(3));

        var response = await student.PostAsJsonAsync(
            $"/api/v1/assignments/{assignment}/submit",
            new CreateSubmissionRequest("My answer, handed in on time.", null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<SubmissionDto>();
        created!.Status.Should().Be(SubmissionStatus.Submitted);
    }

    [Fact]
    public async Task Submit_After_Deadline_Without_LateAllowed_Returns_409()
    {
        var student = await _factory.AsStudentAsync();
        var assignment = await CreateAssignmentAsync(
            deadline: DateTime.UtcNow.AddDays(-1), allowLate: false);

        var response = await student.PostAsJsonAsync(
            $"/api/v1/assignments/{assignment}/submit",
            new CreateSubmissionRequest("Too late.", null));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Submit_After_Deadline_With_LateAllowed_Creates_Submission_With_Status_Late()
    {
        var student = await _factory.AsStudentAsync();
        var assignment = await CreateAssignmentAsync(
            deadline: DateTime.UtcNow.AddDays(-1), allowLate: true);

        var response = await student.PostAsJsonAsync(
            $"/api/v1/assignments/{assignment}/submit",
            new CreateSubmissionRequest("Late, but accepted.", null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<SubmissionDto>();

        created!.Status.Should().Be(SubmissionStatus.Late,
            "late work is accepted but permanently distinguishable from work that arrived on time");
    }

    // ---------------------------------------------------------------------------------
    // Business rule 6 — one submission per student per assignment
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task Second_Submit_To_Same_Assignment_Returns_409()
    {
        var student = await _factory.AsStudentAsync();
        var assignment = await CreateAssignmentAsync();

        var first = await student.PostAsJsonAsync($"/api/v1/assignments/{assignment}/submit",
            new CreateSubmissionRequest("First attempt.", null));

        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await student.PostAsJsonAsync($"/api/v1/assignments/{assignment}/submit",
            new CreateSubmissionRequest("Second attempt.", null));

        second.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "revisions go through the update endpoint, so the two rules never fight");
    }

    // ---------------------------------------------------------------------------------
    // Business rule 7 — the update window
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task Update_Before_Deadline_When_Allowed_Succeeds()
    {
        var student = await _factory.AsStudentAsync();
        var assignment = await CreateAssignmentAsync(
            deadline: DateTime.UtcNow.AddDays(3), allowUpdate: true);

        var submission = await SubmitAsync(student, assignment, "First draft.");

        var response = await student.PutAsJsonAsync($"/api/v1/submissions/{submission.Id}",
            new UpdateSubmissionRequest("Revised answer.", null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<SubmissionDto>();
        updated!.AnswerText.Should().Be("Revised answer.");
    }

    [Fact]
    public async Task Update_When_AllowUpdateBeforeDeadline_False_Returns_409()
    {
        var student = await _factory.AsStudentAsync();
        var assignment = await CreateAssignmentAsync(
            deadline: DateTime.UtcNow.AddDays(3), allowUpdate: false);

        var submission = await SubmitAsync(student, assignment, "Final on first submit.");

        var response = await student.PutAsJsonAsync($"/api/v1/submissions/{submission.Id}",
            new UpdateSubmissionRequest("Trying to revise.", null));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_After_Deadline_Returns_409()
    {
        var student = await _factory.AsStudentAsync();

        // Submit while the window is open, then move the deadline into the past.
        var assignment = await CreateAssignmentAsync(
            deadline: DateTime.UtcNow.AddDays(1), allowUpdate: true);

        var submission = await SubmitAsync(student, assignment, "On time.");

        await _factory.WithDbAsync(async db =>
        {
            var entity = await db.Assignments.SingleAsync(a => a.Id == assignment);
            entity.Deadline = DateTime.UtcNow.AddHours(-1);
            await db.SaveChangesAsync();
        });

        var response = await student.PutAsJsonAsync($"/api/v1/submissions/{submission.Id}",
            new UpdateSubmissionRequest("Sneaking in a change.", null));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "the update window closes at the deadline even when updates are permitted");
    }

    [Fact]
    public async Task Update_After_Grading_Returns_409()
    {
        var student = await _factory.AsStudentAsync();

        // Open on every other axis: updates permitted, deadline days away. Grading is
        // the only thing that closes the window here, so this isolates that rule.
        var assignment = await CreateAssignmentAsync(
            deadline: DateTime.UtcNow.AddDays(3), allowUpdate: true);

        var submission = await SubmitAsync(student, assignment, "Work to be graded.");

        var teacher = await _factory.AsTeacherAsync();
        var grade = await teacher.PutAsJsonAsync($"/api/v1/submissions/{submission.Id}/grade",
            new GradeSubmissionRequest(88, "Well argued."));

        grade.EnsureSuccessStatusCode();

        var response = await student.PutAsJsonAsync($"/api/v1/submissions/{submission.Id}",
            new UpdateSubmissionRequest("Rewriting after seeing the mark.", null));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "marks must always describe the content that was graded");
    }

    // ---------------------------------------------------------------------------------
    // Business rule 8 — a student owns only their own submission
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task Student_Cannot_Read_Another_Students_Submission_Returns_403()
    {
        var student = await _factory.AsStudentAsync();

        var otherStudentsSubmission = await _factory.WithDbAsync(db => db.Submissions
            .Where(s => s.Student.Email == ApiClientExtensions.SecondStudentEmail)
            .Select(s => s.Id)
            .FirstAsync());

        var response = await student.GetAsync($"/api/v1/submissions/{otherStudentsSubmission}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Student_Cannot_Update_Another_Students_Submission_Returns_403()
    {
        var student = await _factory.AsStudentAsync();

        var otherStudentsSubmission = await _factory.WithDbAsync(db => db.Submissions
            .Where(s => s.Student.Email == ApiClientExtensions.SecondStudentEmail)
            .Select(s => s.Id)
            .FirstAsync());

        var response = await student.PutAsJsonAsync(
            $"/api/v1/submissions/{otherStudentsSubmission}",
            new UpdateSubmissionRequest("Editing someone else's work.", null));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "ownership is checked before the update window, so a student cannot even "
            + "learn whether another student's submission is still editable");
    }

    [Fact]
    public async Task MySubmissions_Returns_Only_Own_Submissions_With_Marks_And_Feedback()
    {
        var student = await _factory.AsStudentAsync();

        var studentId = await _factory.WithDbAsync(db => db.Users
            .Where(u => u.Email == ApiClientExtensions.StudentEmail)
            .Select(u => u.Id)
            .SingleAsync());

        var mine = await student.GetFromJsonAsync<PagedResult<SubmissionDto>>(
            "/api/v1/submissions/mine?pageSize=100");

        mine!.Items.Should().NotBeEmpty();
        mine.Items.Should().OnlyContain(s => s.StudentId == studentId);

        // The graded seed submission belongs to student2, so grade a fresh one here to
        // prove marks and feedback reach the owning student.
        var assignment = await CreateAssignmentAsync(maxMarks: 40);
        var submission = await SubmitAsync(student, assignment, "Work to be graded.");

        var teacher = await _factory.AsTeacherAsync();
        await teacher.PutAsJsonAsync($"/api/v1/submissions/{submission.Id}/grade",
            new GradeSubmissionRequest(33, "Solid answer."));

        var after = await student.GetFromJsonAsync<PagedResult<SubmissionDto>>(
            $"/api/v1/submissions/mine?assignmentId={assignment}");

        var graded = after!.Items.Single();
        graded.Marks.Should().Be(33);
        graded.MaxMarks.Should().Be(40);
        graded.Feedback.Should().Be("Solid answer.");
        graded.Status.Should().Be(SubmissionStatus.Graded);
    }

    [Fact]
    public async Task Available_List_Reports_Whether_The_Student_Has_Submitted()
    {
        var student = await _factory.AsStudentAsync();
        var assignment = await CreateAssignmentAsync();

        var before = await student.GetFromJsonAsync<StudentAssignmentDto>(
            $"/api/v1/assignments/{assignment}");

        before!.HasSubmitted.Should().BeFalse();

        await SubmitAsync(student, assignment, "Now submitted.");

        var after = await student.GetFromJsonAsync<StudentAssignmentDto>(
            $"/api/v1/assignments/{assignment}");

        after!.HasSubmitted.Should().BeTrue(
            "the list screen needs this to disable the submit button without a second request");
        after.SubmissionId.Should().NotBeNull();
    }

    // ---------------------------------------------------------------------------------
    // Filters — a student's routes accept only the filters they can honour
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task Available_Can_Be_Filtered_By_Teacher()
    {
        var student = await _factory.AsStudentAsync();

        var teacherId = await _factory.WithDbAsync(db => db.Users
            .Where(u => u.Email == ApiClientExtensions.TeacherEmail)
            .Select(u => u.Id)
            .SingleAsync());

        var filtered = await student.GetFromJsonAsync<PagedResult<StudentAssignmentDto>>(
            $"/api/v1/assignments/available?teacherId={teacherId}&pageSize=100");

        filtered!.Items.Should().NotBeEmpty();
        filtered.Items.Should().OnlyContain(a => a.TeacherName == "Imran Chowdhury");
    }

    [Fact]
    public async Task Available_Filtered_By_A_Teacher_Who_Teaches_Nothing_Here_Is_Empty()
    {
        var student = await _factory.AsStudentAsync();

        // teacher2 teaches only in MATH-201, where this student is not enrolled. The
        // filter narrows within the student's own scope; it cannot widen it.
        var otherTeacherId = await _factory.WithDbAsync(db => db.Users
            .Where(u => u.Email == ApiClientExtensions.SecondTeacherEmail)
            .Select(u => u.Id)
            .SingleAsync());

        var filtered = await student.GetFromJsonAsync<PagedResult<StudentAssignmentDto>>(
            $"/api/v1/assignments/available?teacherId={otherTeacherId}&pageSize=100");

        filtered!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Available_Rejects_A_Status_Filter_With_400()
    {
        var student = await _factory.AsStudentAsync();

        var response = await student.GetAsync("/api/v1/assignments/available?status=Draft");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "answering 200 would imply the status filter had been applied, when a "
            + "student's list is always Published");

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.Should().Be("Unknown query parameter");
    }

    [Fact]
    public async Task MySubmissions_Rejects_A_StudentId_Filter_With_400()
    {
        var student = await _factory.AsStudentAsync();

        var otherStudentId = await _factory.WithDbAsync(db => db.Users
            .Where(u => u.Email == ApiClientExtensions.SecondStudentEmail)
            .Select(u => u.Id)
            .SingleAsync());

        var response = await student.GetAsync(
            $"/api/v1/submissions/mine?studentId={otherStudentId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "silently returning the caller's own work would look like the filter applied");
    }

    [Fact]
    public async Task Available_Still_Accepts_The_Filters_It_Does_Support()
    {
        // The guard must not have made the supported filters unreachable.
        var student = await _factory.AsStudentAsync();

        var response = await student.GetAsync(
            "/api/v1/assignments/available?search=a&page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ---------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Inserts an assignment in CS-101 with exactly the deadline and flags a test needs.
    /// Going through the teacher API would not do: creating with a past deadline is
    /// rejected by validation, which is the very state several of these tests require.
    /// </summary>
    private async Task<Guid> CreateAssignmentAsync(
        DateTime? deadline = null,
        bool allowLate = false,
        bool allowUpdate = true,
        int maxMarks = 100,
        AssignmentStatus status = AssignmentStatus.Published)
    {
        var id = Guid.NewGuid();

        await _factory.WithDbAsync(async db =>
        {
            var subject = await db.Subjects.SingleAsync(s => s.Code == "DS-101");

            var teacherId = await db.Users
                .Where(u => u.Email == ApiClientExtensions.TeacherEmail)
                .Select(u => u.Id)
                .SingleAsync();

            db.Assignments.Add(new Assignment
            {
                Id = id,
                Title = $"Student test assignment {id:N}"[..40],
                Description = "Created by an integration test.",
                Deadline = deadline ?? DateTime.UtcNow.AddDays(7),
                MaxMarks = maxMarks,
                Status = status,
                ClassCourseId = subject.ClassCourseId,
                SubjectId = subject.Id,
                CreatedByTeacherId = teacherId,
                AllowLateSubmission = allowLate,
                AllowUpdateBeforeDeadline = allowUpdate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        });

        return id;
    }

    private static async Task<SubmissionDto> SubmitAsync(
        HttpClient student, Guid assignmentId, string answer)
    {
        var response = await student.PostAsJsonAsync(
            $"/api/v1/assignments/{assignmentId}/submit",
            new CreateSubmissionRequest(answer, null));

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<SubmissionDto>())!;
    }
}
