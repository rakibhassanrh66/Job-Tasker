// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Common.Security;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Exceptions;
using AssignmentSystem.Infrastructure.Persistence;
using AssignmentSystem.UnitTests.TestSupport;
using FluentAssertions;
using Xunit;

namespace AssignmentSystem.UnitTests.Security;

/// <summary>
/// The ownership gate, tested in isolation.
///
/// Each check is verified both ways round. A test that only proves the owner is allowed
/// through would pass just as happily against a method that always returns — which is the
/// exact failure mode that matters here.
/// </summary>
public class ResourceAuthorizerTests
{
    private readonly AppDbContext _db = TestDb.Create();

    private ResourceAuthorizer CreateAuthorizer() => new(_db);

    // ---------------------------------------------------------------------------------
    // Business rule 4 — teacher owns the assignment
    // ---------------------------------------------------------------------------------

    [Fact]
    public void Teacher_Who_Created_The_Assignment_Is_Allowed()
    {
        var teacherId = Guid.NewGuid();
        var assignment = new Assignment { CreatedByTeacherId = teacherId };

        var act = () => CreateAuthorizer().EnsureTeacherOwnsAssignment(teacherId, assignment);

        act.Should().NotThrow();
    }

    [Fact]
    public void Teacher_Who_Did_Not_Create_The_Assignment_Is_Forbidden()
    {
        var assignment = new Assignment { CreatedByTeacherId = Guid.NewGuid() };
        var otherTeacherId = Guid.NewGuid();

        var act = () => CreateAuthorizer().EnsureTeacherOwnsAssignment(otherTeacherId, assignment);

        act.Should().Throw<ForbiddenResourceException>()
            .Which.StatusCode.Should().Be(403);
    }

    // ---------------------------------------------------------------------------------
    // Business rule 3 — teacher teaches this subject in this class
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task Teacher_Allocated_To_The_Subject_And_Class_Is_Allowed()
    {
        var teacherId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var classId = Guid.NewGuid();

        _db.TeacherAssignments.Add(new TeacherAssignment
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            SubjectId = subjectId,
            ClassCourseId = classId,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        var act = async () => await CreateAuthorizer()
            .EnsureTeacherTeachesSubjectInClassAsync(teacherId, subjectId, classId);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Teacher_Not_Allocated_To_The_Pair_Is_Forbidden()
    {
        var act = async () => await CreateAuthorizer()
            .EnsureTeacherTeachesSubjectInClassAsync(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<ForbiddenResourceException>();
    }

    [Fact]
    public async Task Teaching_The_Subject_In_A_Different_Class_Is_Not_Enough()
    {
        // The allocation is per (subject, class) pair, not per subject. Teaching Data
        // Structures in CS-101 grants nothing in MATH-201.
        var teacherId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();

        _db.TeacherAssignments.Add(new TeacherAssignment
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            SubjectId = subjectId,
            ClassCourseId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        var act = async () => await CreateAuthorizer()
            .EnsureTeacherTeachesSubjectInClassAsync(teacherId, subjectId, Guid.NewGuid());

        await act.Should().ThrowAsync<ForbiddenResourceException>();
    }

    // ---------------------------------------------------------------------------------
    // Business rule 8 — student owns the submission
    // ---------------------------------------------------------------------------------

    [Fact]
    public void Student_Who_Owns_The_Submission_Is_Allowed()
    {
        var studentId = Guid.NewGuid();
        var submission = new Submission { StudentId = studentId };

        var act = () => CreateAuthorizer().EnsureStudentOwnsSubmission(studentId, submission);

        act.Should().NotThrow();
    }

    [Fact]
    public void Student_Reaching_Another_Students_Submission_Is_Forbidden()
    {
        var submission = new Submission { StudentId = Guid.NewGuid() };

        var act = () => CreateAuthorizer()
            .EnsureStudentOwnsSubmission(Guid.NewGuid(), submission);

        act.Should().Throw<ForbiddenResourceException>()
            .Which.StatusCode.Should().Be(403);
    }

    // ---------------------------------------------------------------------------------
    // Business rule 2 — student enrolled in the class
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task Enrolled_Student_Is_Allowed()
    {
        var studentId = Guid.NewGuid();
        var classId = Guid.NewGuid();

        _db.Enrollments.Add(new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            ClassCourseId = classId,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        var act = async () => await CreateAuthorizer()
            .EnsureStudentEnrolledInClassAsync(studentId, classId);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Student_Not_Enrolled_In_The_Class_Is_Forbidden()
    {
        var studentId = Guid.NewGuid();

        _db.Enrollments.Add(new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            ClassCourseId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        var act = async () => await CreateAuthorizer()
            .EnsureStudentEnrolledInClassAsync(studentId, Guid.NewGuid());

        await act.Should().ThrowAsync<ForbiddenResourceException>();
    }

    [Fact]
    public async Task Another_Students_Enrolment_Does_Not_Grant_Access()
    {
        var classId = Guid.NewGuid();

        _db.Enrollments.Add(new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            ClassCourseId = classId,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        var act = async () => await CreateAuthorizer()
            .EnsureStudentEnrolledInClassAsync(Guid.NewGuid(), classId);

        await act.Should().ThrowAsync<ForbiddenResourceException>();
    }
}
