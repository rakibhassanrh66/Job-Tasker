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

namespace AssignmentSystem.IntegrationTests.Teacher;

/// <summary>
/// Business rules 3, 4, 9, 10 and 11 over real HTTP.
///
/// The seed data is what makes the negative cases possible: teacher2 teaches only in
/// MATH-201, so there is a teacher who provably does not teach CS-101 and the ownership
/// rules can be tested for refusal rather than only for success.
/// </summary>
[Collection(ApiCollection.Name)]
public class TeacherModuleTests
{
    private readonly ApiFactory _factory;

    public TeacherModuleTests(ApiFactory factory) => _factory = factory;

    // ---------------------------------------------------------------------------------
    // Business rule 3 — a teacher may only create where they are allocated
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task Teacher_Can_Create_Assignment_For_Assigned_SubjectClass()
    {
        var teacher = await _factory.AsTeacherAsync();
        var (subjectId, classId) = await SubjectAndClassAsync("DS-101");

        var response = await teacher.PostAsJsonAsync("/api/v1/assignments",
            NewAssignment(subjectId, classId, "Allocated subject"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<AssignmentDto>();

        created!.Status.Should().Be(AssignmentStatus.Draft,
            "assignments are created as drafts so nothing reaches students by accident");
    }

    [Fact]
    public async Task Teacher_Cannot_Create_Assignment_For_Unassigned_SubjectClass_Returns_403()
    {
        // teacher1 teaches in CS-101 only; LA-201 belongs to MATH-201.
        var teacher = await _factory.AsTeacherAsync();
        var (subjectId, classId) = await SubjectAndClassAsync("LA-201");

        var response = await teacher.PostAsJsonAsync("/api/v1/assignments",
            NewAssignment(subjectId, classId, "Not my subject"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Creating_With_A_Subject_From_Another_Class_Returns_422()
    {
        var teacher = await _factory.AsTeacherAsync();
        var (subjectId, _) = await SubjectAndClassAsync("DS-101");
        var (_, otherClassId) = await SubjectAndClassAsync("LA-201");

        var response = await teacher.PostAsJsonAsync("/api/v1/assignments",
            NewAssignment(subjectId, otherClassId, "Mismatched pair"));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Creating_With_A_Deadline_In_The_Past_Returns_422()
    {
        var teacher = await _factory.AsTeacherAsync();
        var (subjectId, classId) = await SubjectAndClassAsync("DS-101");

        var request = NewAssignment(subjectId, classId, "Already closed")
            with { Deadline = DateTime.UtcNow.AddDays(-1) };

        var response = await teacher.PostAsJsonAsync("/api/v1/assignments", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "an assignment nobody could ever submit to is not a useful thing to create");
    }

    [Fact]
    public async Task Creating_With_Zero_MaxMarks_Returns_422()
    {
        var teacher = await _factory.AsTeacherAsync();
        var (subjectId, classId) = await SubjectAndClassAsync("DS-101");

        var request = NewAssignment(subjectId, classId, "Worth nothing") with { MaxMarks = 0 };

        var response = await teacher.PostAsJsonAsync("/api/v1/assignments", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ---------------------------------------------------------------------------------
    // Business rule 11 — publish transitions
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task Publish_Draft_Assignment_Succeeds()
    {
        var teacher = await _factory.AsTeacherAsync();
        var draft = await CreateAssignmentAsync(teacher, "Publishable");

        var response = await teacher.PostAsync($"/api/v1/assignments/{draft.Id}/publish", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var published = await response.Content.ReadFromJsonAsync<AssignmentDto>();
        published!.Status.Should().Be(AssignmentStatus.Published);
    }

    [Fact]
    public async Task Publish_Already_Published_Returns_409()
    {
        var teacher = await _factory.AsTeacherAsync();
        var draft = await CreateAssignmentAsync(teacher, "Published twice");

        await teacher.PostAsync($"/api/v1/assignments/{draft.Id}/publish", null);

        var second = await teacher.PostAsync($"/api/v1/assignments/{draft.Id}/publish", null);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "re-publishing is a 409 rather than a silent no-op");
    }

    [Fact]
    public async Task Publish_Archived_Assignment_Returns_409()
    {
        var teacher = await _factory.AsTeacherAsync();
        var draft = await CreateAssignmentAsync(teacher, "Archived then published");

        await _factory.WithDbAsync(async db =>
        {
            var entity = await db.Assignments.SingleAsync(a => a.Id == draft.Id);
            entity.Status = AssignmentStatus.Archived;
            await db.SaveChangesAsync();
        });

        var response = await teacher.PostAsync($"/api/v1/assignments/{draft.Id}/publish", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ---------------------------------------------------------------------------------
    // Business rule 4 — a teacher may only act on their own assignments
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task Teacher_Cannot_Update_Another_Teachers_Assignment_Returns_403()
    {
        var teacher1 = await _factory.AsTeacherAsync();
        var teacher2 = await _factory.AuthenticatedClientAsync(
            ApiClientExtensions.SecondTeacherEmail, ApiClientExtensions.TeacherPassword);

        var owned = await CreateAssignmentAsync(teacher1, "Belongs to teacher one");

        var response = await teacher2.PutAsJsonAsync($"/api/v1/assignments/{owned.Id}",
            new UpdateAssignmentRequest(
                "Hijacked", "Edited by someone else",
                DateTime.UtcNow.AddDays(5), 50, false, true));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Teacher_Cannot_Delete_Another_Teachers_Assignment_Returns_403()
    {
        var teacher1 = await _factory.AsTeacherAsync();
        var teacher2 = await _factory.AuthenticatedClientAsync(
            ApiClientExtensions.SecondTeacherEmail, ApiClientExtensions.TeacherPassword);

        var owned = await CreateAssignmentAsync(teacher1, "Not yours to delete");

        var response = await teacher2.DeleteAsync($"/api/v1/assignments/{owned.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Teacher_Cannot_Publish_Another_Teachers_Assignment_Returns_403()
    {
        var teacher1 = await _factory.AsTeacherAsync();
        var teacher2 = await _factory.AuthenticatedClientAsync(
            ApiClientExtensions.SecondTeacherEmail, ApiClientExtensions.TeacherPassword);

        var owned = await CreateAssignmentAsync(teacher1, "Not yours to publish");

        var response = await teacher2.PostAsync($"/api/v1/assignments/{owned.Id}/publish", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Teacher_Cannot_List_Submissions_Of_Another_Teachers_Assignment_Returns_403()
    {
        var teacher1 = await _factory.AsTeacherAsync();
        var teacher2 = await _factory.AuthenticatedClientAsync(
            ApiClientExtensions.SecondTeacherEmail, ApiClientExtensions.TeacherPassword);

        var owned = await CreateAssignmentAsync(teacher1, "Private submissions");

        var response = await teacher2.GetAsync($"/api/v1/assignments/{owned.Id}/submissions");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "student work must not be readable by a teacher who did not set the assignment");
    }

    [Fact]
    public async Task Teacher_Cannot_Grade_Submission_Of_Another_Teachers_Assignment_Returns_403()
    {
        var teacher1 = await _factory.AsTeacherAsync();
        var teacher2 = await _factory.AuthenticatedClientAsync(
            ApiClientExtensions.SecondTeacherEmail, ApiClientExtensions.TeacherPassword);

        var owned = await CreateAssignmentAsync(teacher1, "Graded by owner only");
        var submissionId = await AddSubmissionAsync(owned.Id);

        var response = await teacher2.PutAsJsonAsync(
            $"/api/v1/submissions/{submissionId}/grade",
            new GradeSubmissionRequest(50, "Nice try"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Teacher_Only_Sees_Their_Own_Assignments_In_Mine()
    {
        var teacher2 = await _factory.AuthenticatedClientAsync(
            ApiClientExtensions.SecondTeacherEmail, ApiClientExtensions.TeacherPassword);

        var mine = await teacher2.GetFromJsonAsync<PagedResult<AssignmentDto>>(
            "/api/v1/assignments/mine?pageSize=100");

        var teacher2Id = await _factory.WithDbAsync(db => db.Users
            .Where(u => u.Email == ApiClientExtensions.SecondTeacherEmail)
            .Select(u => u.Id)
            .SingleAsync());

        mine!.Items.Should().NotBeEmpty();
        mine.Items.Should().OnlyContain(a => a.CreatedByTeacherId == teacher2Id);
    }

    // ---------------------------------------------------------------------------------
    // Business rule 9 — marks bounds
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task Grade_With_Negative_Marks_Returns_422()
    {
        var teacher = await _factory.AsTeacherAsync();
        var assignment = await CreateAssignmentAsync(teacher, "Negative marks", maxMarks: 100);
        var submissionId = await AddSubmissionAsync(assignment.Id);

        var response = await teacher.PutAsJsonAsync(
            $"/api/v1/submissions/{submissionId}/grade",
            new GradeSubmissionRequest(-1, null));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Grade_With_Marks_Above_MaxMarks_Returns_422()
    {
        var teacher = await _factory.AsTeacherAsync();
        var assignment = await CreateAssignmentAsync(teacher, "Over maximum", maxMarks: 20);
        var submissionId = await AddSubmissionAsync(assignment.Id);

        var response = await teacher.PutAsJsonAsync(
            $"/api/v1/submissions/{submissionId}/grade",
            new GradeSubmissionRequest(21, null));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.Should().Be("Marks out of range");
    }

    [Fact]
    public async Task Grade_With_Marks_Equal_To_MaxMarks_Succeeds()
    {
        // The boundary itself, where an off-by-one would hide.
        var teacher = await _factory.AsTeacherAsync();
        var assignment = await CreateAssignmentAsync(teacher, "Exactly maximum", maxMarks: 20);
        var submissionId = await AddSubmissionAsync(assignment.Id);

        var response = await teacher.PutAsJsonAsync(
            $"/api/v1/submissions/{submissionId}/grade",
            new GradeSubmissionRequest(20, "Full marks"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var graded = await response.Content.ReadFromJsonAsync<SubmissionDto>();
        graded!.Marks.Should().Be(20);
    }

    [Fact]
    public async Task Grade_With_Zero_Marks_Succeeds()
    {
        // The other boundary. Zero is a legitimate mark, not a missing one.
        var teacher = await _factory.AsTeacherAsync();
        var assignment = await CreateAssignmentAsync(teacher, "Zero marks", maxMarks: 20);
        var submissionId = await AddSubmissionAsync(assignment.Id);

        var response = await teacher.PutAsJsonAsync(
            $"/api/v1/submissions/{submissionId}/grade",
            new GradeSubmissionRequest(0, "Not attempted"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var graded = await response.Content.ReadFromJsonAsync<SubmissionDto>();
        graded!.Marks.Should().Be(0);
    }

    [Fact]
    public async Task Grading_Sets_GradedByTeacherId_And_GradedAt()
    {
        var teacher = await _factory.AsTeacherAsync();
        var assignment = await CreateAssignmentAsync(teacher, "Attribution", maxMarks: 50);
        var submissionId = await AddSubmissionAsync(assignment.Id);

        await teacher.PutAsJsonAsync($"/api/v1/submissions/{submissionId}/grade",
            new GradeSubmissionRequest(42, "Good work"));

        var teacherId = await _factory.WithDbAsync(db => db.Users
            .Where(u => u.Email == ApiClientExtensions.TeacherEmail)
            .Select(u => u.Id)
            .SingleAsync());

        var stored = await _factory.WithDbAsync(db =>
            db.Submissions.AsNoTracking().SingleAsync(s => s.Id == submissionId));

        stored.Status.Should().Be(SubmissionStatus.Graded);
        stored.Marks.Should().Be(42);
        stored.Feedback.Should().Be("Good work");
        stored.GradedByTeacherId.Should().Be(teacherId, "who graded it must be recorded");
        stored.GradedAt.Should().NotBeNull();
    }

    // ---------------------------------------------------------------------------------
    // Business rule 10 — status transitions
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(SubmissionStatus.Submitted)]
    [InlineData(SubmissionStatus.UnderReview)]
    [InlineData(SubmissionStatus.Late)]
    public async Task Grade_Endpoint_Accepts_Submitted_UnderReview_And_Late(SubmissionStatus start)
    {
        var teacher = await _factory.AsTeacherAsync();
        var assignment = await CreateAssignmentAsync(teacher, $"Gradeable from {start}", maxMarks: 30);
        var submissionId = await AddSubmissionAsync(assignment.Id, start);

        var response = await teacher.PutAsJsonAsync(
            $"/api/v1/submissions/{submissionId}/grade",
            new GradeSubmissionRequest(15, null));

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"entering marks is itself the review, so {start} is gradeable directly");
    }

    [Fact]
    public async Task Grading_An_Already_Graded_Submission_Returns_409()
    {
        var teacher = await _factory.AsTeacherAsync();
        var assignment = await CreateAssignmentAsync(teacher, "Graded once", maxMarks: 30);
        var submissionId = await AddSubmissionAsync(assignment.Id);

        await teacher.PutAsJsonAsync($"/api/v1/submissions/{submissionId}/grade",
            new GradeSubmissionRequest(15, null));

        var second = await teacher.PutAsJsonAsync($"/api/v1/submissions/{submissionId}/grade",
            new GradeSubmissionRequest(16, null));

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData(SubmissionStatus.Submitted, SubmissionStatus.UnderReview, HttpStatusCode.OK)]
    [InlineData(SubmissionStatus.Submitted, SubmissionStatus.Returned, HttpStatusCode.OK)]
    [InlineData(SubmissionStatus.UnderReview, SubmissionStatus.Graded, HttpStatusCode.OK)]
    [InlineData(SubmissionStatus.Graded, SubmissionStatus.Returned, HttpStatusCode.OK)]
    [InlineData(SubmissionStatus.Submitted, SubmissionStatus.Late, HttpStatusCode.Conflict)]
    [InlineData(SubmissionStatus.Submitted, SubmissionStatus.Submitted, HttpStatusCode.Conflict)]
    [InlineData(SubmissionStatus.Returned, SubmissionStatus.UnderReview, HttpStatusCode.Conflict)]
    [InlineData(SubmissionStatus.Graded, SubmissionStatus.UnderReview, HttpStatusCode.Conflict)]
    public async Task Status_Transitions_Are_Enforced(
        SubmissionStatus from, SubmissionStatus to, HttpStatusCode expected)
    {
        var teacher = await _factory.AsTeacherAsync();
        var assignment = await CreateAssignmentAsync(teacher, $"Transition {from} to {to}");
        var submissionId = await AddSubmissionAsync(assignment.Id, from);

        var response = await teacher.PutAsJsonAsync(
            $"/api/v1/submissions/{submissionId}/status",
            new ChangeSubmissionStatusRequest(to));

        response.StatusCode.Should().Be(expected, $"{from} -> {to}");
    }

    // ---------------------------------------------------------------------------------
    // Deleting an assignment with submissions
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task Deleting_An_Assignment_With_Submissions_Returns_409()
    {
        // The foreign key cascades, so deleting would take students' work with it.
        var teacher = await _factory.AsTeacherAsync();
        var assignment = await CreateAssignmentAsync(teacher, "Has submissions");
        await AddSubmissionAsync(assignment.Id);

        var response = await teacher.DeleteAsync($"/api/v1/assignments/{assignment.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Deleting_An_Untouched_Assignment_Succeeds()
    {
        var teacher = await _factory.AsTeacherAsync();
        var assignment = await CreateAssignmentAsync(teacher, "Nothing submitted");

        var response = await teacher.DeleteAsync($"/api/v1/assignments/{assignment.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ---------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------

    private static CreateAssignmentRequest NewAssignment(
        Guid subjectId, Guid classId, string title, int maxMarks = 100) =>
        new(
            Title: title,
            Description: "Created by an integration test.",
            Deadline: DateTime.UtcNow.AddDays(7),
            MaxMarks: maxMarks,
            ClassCourseId: classId,
            SubjectId: subjectId,
            AllowLateSubmission: false,
            AllowUpdateBeforeDeadline: true);

    private async Task<(Guid SubjectId, Guid ClassId)> SubjectAndClassAsync(string subjectCode)
    {
        var subject = await _factory.WithDbAsync(db =>
            db.Subjects.AsNoTracking().SingleAsync(s => s.Code == subjectCode));

        return (subject.Id, subject.ClassCourseId);
    }

    /// <summary>Creates a draft assignment for teacher1 in a subject they teach.</summary>
    private async Task<AssignmentDto> CreateAssignmentAsync(
        HttpClient teacher, string title, int maxMarks = 100)
    {
        var (subjectId, classId) = await SubjectAndClassAsync("DS-101");

        var response = await teacher.PostAsJsonAsync("/api/v1/assignments",
            NewAssignment(subjectId, classId, $"{title} {Guid.NewGuid():N}"[..40], maxMarks));

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<AssignmentDto>())!;
    }

    /// <summary>
    /// Inserts a submission directly. The student-facing submit endpoint arrives in M5;
    /// these tests are about what a teacher may do with work that already exists.
    /// </summary>
    private async Task<Guid> AddSubmissionAsync(
        Guid assignmentId, SubmissionStatus status = SubmissionStatus.Submitted)
    {
        var id = Guid.NewGuid();

        await _factory.WithDbAsync(async db =>
        {
            var studentId = await db.Users
                .Where(u => u.Email == ApiClientExtensions.StudentEmail)
                .Select(u => u.Id)
                .SingleAsync();

            db.Submissions.Add(new Submission
            {
                Id = id,
                AssignmentId = assignmentId,
                StudentId = studentId,
                AnswerText = "Answer supplied by an integration test.",
                Status = status,
                SubmittedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        });

        return id;
    }
}
