// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Enums;
using AssignmentSystem.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.Application.TeacherAssignments;

public record TeacherAssignmentDto(
    Guid Id,
    Guid TeacherId,
    string TeacherName,
    Guid SubjectId,
    string SubjectName,
    Guid ClassCourseId,
    string ClassCourseCode);

public record CreateTeacherAssignmentRequest(Guid TeacherId, Guid SubjectId, Guid ClassCourseId);

public class TeacherAssignmentListQuery : PagedQuery
{
    public Guid? TeacherId { get; set; }

    public Guid? ClassCourseId { get; set; }
}

public interface ITeacherAssignmentService
{
    Task<PagedResult<TeacherAssignmentDto>> ListAsync(
        TeacherAssignmentListQuery query, CancellationToken cancellationToken = default);

    Task<TeacherAssignmentDto> CreateAsync(
        CreateTeacherAssignmentRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Manages which teacher teaches which subject in which class. This table is the input to
/// business rule 3, so what it is allowed to contain matters: a nonsense allocation here
/// would grant a teacher the right to create assignments somewhere they do not teach.
/// </summary>
public class TeacherAssignmentService : ITeacherAssignmentService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public TeacherAssignmentService(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PagedResult<TeacherAssignmentDto>> ListAsync(
        TeacherAssignmentListQuery query, CancellationToken cancellationToken = default)
    {
        var allocations = _db.TeacherAssignments.AsNoTracking();

        if (query.TeacherId is not null)
        {
            allocations = allocations.Where(t => t.TeacherId == query.TeacherId);
        }

        if (query.ClassCourseId is not null)
        {
            allocations = allocations.Where(t => t.ClassCourseId == query.ClassCourseId);
        }

        return await allocations
            .OrderBy(t => t.ClassCourse.Code)
            .ThenBy(t => t.Subject.Code)
            .Select(t => new TeacherAssignmentDto(
                t.Id,
                t.TeacherId, t.Teacher.FullName,
                t.SubjectId, t.Subject.Name,
                t.ClassCourseId, t.ClassCourse.Code))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<TeacherAssignmentDto> CreateAsync(
        CreateTeacherAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        var teacher = await _db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == request.TeacherId, cancellationToken)
            ?? throw new NotFoundException("Teacher", request.TeacherId);

        if (teacher.Role != UserRole.Teacher)
        {
            throw new ValidationFailedException(
                nameof(request.TeacherId),
                "Only users with the Teacher role can be allocated to teach a subject.");
        }

        var subject = await _db.Subjects
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == request.SubjectId, cancellationToken)
            ?? throw new NotFoundException("Subject", request.SubjectId);

        // A subject belongs to exactly one class. Allocating a teacher to a subject in a
        // class that subject is not part of would create an allocation that rule 3 could
        // match, granting rights over a class/subject pair that does not exist.
        if (subject.ClassCourseId != request.ClassCourseId)
        {
            throw new ValidationFailedException(
                nameof(request.SubjectId),
                "That subject does not belong to the specified class.");
        }

        var alreadyAllocated = await _db.TeacherAssignments.AnyAsync(
            t => t.TeacherId == request.TeacherId
                 && t.SubjectId == request.SubjectId
                 && t.ClassCourseId == request.ClassCourseId,
            cancellationToken);

        if (alreadyAllocated)
        {
            throw DuplicateResourceException.TeacherAllocation();
        }

        var entity = new TeacherAssignment
        {
            Id = Guid.NewGuid(),
            TeacherId = request.TeacherId,
            SubjectId = request.SubjectId,
            ClassCourseId = request.ClassCourseId,
            CreatedAt = _clock.UtcNow
        };

        _db.TeacherAssignments.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return await _db.TeacherAssignments
            .AsNoTracking()
            .Where(t => t.Id == entity.Id)
            .Select(t => new TeacherAssignmentDto(
                t.Id,
                t.TeacherId, t.Teacher.FullName,
                t.SubjectId, t.Subject.Name,
                t.ClassCourseId, t.ClassCourse.Code))
            .SingleAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.TeacherAssignments
            .SingleOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException("Teacher allocation", id);

        _db.TeacherAssignments.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
