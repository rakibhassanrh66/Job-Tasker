// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Enums;
using AssignmentSystem.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.Application.Enrollments;

public record EnrollmentDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string StudentEmail,
    Guid ClassCourseId,
    string ClassCourseCode,
    DateTime CreatedAt);

public record CreateEnrollmentRequest(Guid StudentId, Guid ClassCourseId);

public class EnrollmentListQuery : PagedQuery
{
    public Guid? StudentId { get; set; }

    public Guid? ClassCourseId { get; set; }
}

public interface IEnrollmentService
{
    Task<PagedResult<EnrollmentDto>> ListAsync(
        EnrollmentListQuery query, CancellationToken cancellationToken = default);

    Task<EnrollmentDto> CreateAsync(
        CreateEnrollmentRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Manages class membership. This table decides what a student can see (business rule 2),
/// so removing an enrolment silently removes that student's access to the class's
/// assignments.
/// </summary>
public class EnrollmentService : IEnrollmentService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public EnrollmentService(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PagedResult<EnrollmentDto>> ListAsync(
        EnrollmentListQuery query, CancellationToken cancellationToken = default)
    {
        var enrollments = _db.Enrollments.AsNoTracking();

        if (query.StudentId is not null)
        {
            enrollments = enrollments.Where(e => e.StudentId == query.StudentId);
        }

        if (query.ClassCourseId is not null)
        {
            enrollments = enrollments.Where(e => e.ClassCourseId == query.ClassCourseId);
        }

        return await enrollments
            .OrderBy(e => e.ClassCourse.Code)
            .ThenBy(e => e.Student.FullName)
            .Select(e => new EnrollmentDto(
                e.Id,
                e.StudentId, e.Student.FullName, e.Student.Email,
                e.ClassCourseId, e.ClassCourse.Code,
                e.CreatedAt))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<EnrollmentDto> CreateAsync(
        CreateEnrollmentRequest request, CancellationToken cancellationToken = default)
    {
        var student = await _db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == request.StudentId, cancellationToken)
            ?? throw new NotFoundException("Student", request.StudentId);

        if (student.Role != UserRole.Student)
        {
            throw new ValidationFailedException(
                nameof(request.StudentId),
                "Only users with the Student role can be enrolled in a class.");
        }

        var classExists = await _db.ClassCourses
            .AnyAsync(c => c.Id == request.ClassCourseId, cancellationToken);

        if (!classExists)
        {
            throw new NotFoundException("Class", request.ClassCourseId);
        }

        var alreadyEnrolled = await _db.Enrollments.AnyAsync(
            e => e.StudentId == request.StudentId && e.ClassCourseId == request.ClassCourseId,
            cancellationToken);

        if (alreadyEnrolled)
        {
            throw DuplicateResourceException.Enrollment();
        }

        var entity = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = request.StudentId,
            ClassCourseId = request.ClassCourseId,
            CreatedAt = _clock.UtcNow
        };

        _db.Enrollments.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return await _db.Enrollments
            .AsNoTracking()
            .Where(e => e.Id == entity.Id)
            .Select(e => new EnrollmentDto(
                e.Id,
                e.StudentId, e.Student.FullName, e.Student.Email,
                e.ClassCourseId, e.ClassCourse.Code,
                e.CreatedAt))
            .SingleAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Enrollments.SingleOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new NotFoundException("Enrollment", id);

        _db.Enrollments.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
