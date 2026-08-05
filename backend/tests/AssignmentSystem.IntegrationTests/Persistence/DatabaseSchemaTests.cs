// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Enums;
using AssignmentSystem.Infrastructure.Persistence;
using AssignmentSystem.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AssignmentSystem.IntegrationTests.Persistence;

/// <summary>
/// Asserts the guarantees the schema itself must provide. The service layer will also
/// check these rules, but a constraint in the database is what holds when two requests
/// race or when a future change forgets the service check.
/// </summary>
[Collection(PostgresCollection.Name)]
public class DatabaseSchemaTests
{
    private readonly PostgresFixture _fixture;

    public DatabaseSchemaTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Migration_Creates_All_Tables()
    {
        await using var db = _fixture.CreateContext();

        var tables = await db.Database
            .SqlQueryRaw<string>(
                "SELECT tablename AS \"Value\" FROM pg_tables WHERE schemaname = 'public'")
            .ToListAsync();

        tables.Should().Contain(new[]
        {
            "Users", "ClassCourses", "Subjects", "Enrollments",
            "TeacherAssignments", "Assignments", "Submissions", "RefreshTokens"
        });
    }

    [Fact]
    public async Task Duplicate_Email_Violates_Unique_Index()
    {
        await using var db = _fixture.CreateContext();

        var email = $"duplicate-{Guid.NewGuid():N}@demo.test";

        db.Users.Add(NewUser(email));
        await db.SaveChangesAsync();

        db.Users.Add(NewUser(email));

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>(
            "User.Email carries a unique index");
    }

    [Fact]
    public async Task Duplicate_Submission_Violates_Unique_Index()
    {
        // Business rule 6, at the level that actually guarantees it. The service checks
        // for an existing submission first, but between that check and the insert two
        // concurrent requests could both proceed — only the index stops the second.
        await using var db = _fixture.CreateContext();

        var (assignment, student) = await CreateAssignmentWithStudentAsync(db);

        db.Submissions.Add(NewSubmission(assignment.Id, student.Id));
        await db.SaveChangesAsync();

        db.Submissions.Add(NewSubmission(assignment.Id, student.Id));

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>(
            "(AssignmentId, StudentId) is unique — one submission per student per assignment");
    }

    [Fact]
    public async Task Negative_Marks_Violates_Check_Constraint()
    {
        await using var db = _fixture.CreateContext();

        var (assignment, student) = await CreateAssignmentWithStudentAsync(db);

        var submission = NewSubmission(assignment.Id, student.Id);
        submission.Marks = -1;

        db.Submissions.Add(submission);

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>(
            "CK_Submissions_Marks_NonNegative rejects negative marks");
    }

    [Fact]
    public async Task MaxMarks_Must_Be_Positive()
    {
        await using var db = _fixture.CreateContext();

        var teacher = NewUser($"teacher-{Guid.NewGuid():N}@demo.test", UserRole.Teacher);
        var (classCourse, subject) = NewClassAndSubject();

        db.Users.Add(teacher);
        db.ClassCourses.Add(classCourse);
        db.Subjects.Add(subject);
        await db.SaveChangesAsync();

        var assignment = NewAssignment(classCourse.Id, subject.Id, teacher.Id);
        assignment.MaxMarks = 0;

        db.Assignments.Add(assignment);

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>(
            "CK_Assignments_MaxMarks_Positive requires MaxMarks > 0");
    }

    // ---------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------

    private static User NewUser(string email, UserRole role = UserRole.Student) => new()
    {
        Id = Guid.NewGuid(),
        Email = email,
        FullName = "Schema Test User",
        PasswordHash = "not-a-real-hash",
        Role = role,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    private static (ClassCourse, Subject) NewClassAndSubject()
    {
        var classCourse = new ClassCourse
        {
            Id = Guid.NewGuid(),
            Name = "Schema Test Class",
            Code = $"TST-{Guid.NewGuid():N}"[..12]
        };

        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            Name = "Schema Test Subject",
            Code = $"SUB-{Guid.NewGuid():N}"[..12],
            ClassCourseId = classCourse.Id
        };

        return (classCourse, subject);
    }

    private static Assignment NewAssignment(Guid classId, Guid subjectId, Guid teacherId) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Schema Test Assignment",
        Description = "Created by a schema test.",
        Deadline = DateTime.UtcNow.AddDays(7),
        MaxMarks = 100,
        Status = AssignmentStatus.Published,
        ClassCourseId = classId,
        SubjectId = subjectId,
        CreatedByTeacherId = teacherId,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static Submission NewSubmission(Guid assignmentId, Guid studentId) => new()
    {
        Id = Guid.NewGuid(),
        AssignmentId = assignmentId,
        StudentId = studentId,
        AnswerText = "Schema test answer.",
        Status = SubmissionStatus.Submitted,
        SubmittedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    /// <summary>Creates the minimum graph a submission needs: a teacher, a class, a
    /// subject, an assignment and a student.</summary>
    private static async Task<(Assignment Assignment, User Student)> CreateAssignmentWithStudentAsync(
        AppDbContext db)
    {
        var teacher = NewUser($"teacher-{Guid.NewGuid():N}@demo.test", UserRole.Teacher);
        var student = NewUser($"student-{Guid.NewGuid():N}@demo.test");
        var (classCourse, subject) = NewClassAndSubject();

        db.Users.AddRange(teacher, student);
        db.ClassCourses.Add(classCourse);
        db.Subjects.Add(subject);
        await db.SaveChangesAsync();

        var assignment = NewAssignment(classCourse.Id, subject.Id, teacher.Id);
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();

        return (assignment, student);
    }
}
