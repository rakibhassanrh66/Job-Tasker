// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Assignments.Dtos;
using AssignmentSystem.Application.Common;
using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.Application.Assignments;

public partial interface IAssignmentService
{
    /// <summary>
    /// Unfiltered listing for administrative oversight. Deliberately not scoped by class
    /// or status — the admin role is the only caller, and the point is visibility across
    /// the whole system. Teacher and student listings are separate methods with their own
    /// scoping, so this one can never be reached with a lesser role by mistake.
    /// </summary>
    Task<PagedResult<AssignmentDto>> ListAllAsync(
        AssignmentListQuery query, CancellationToken cancellationToken = default);
}

public partial class AssignmentService : IAssignmentService
{
    private readonly IAppDbContext _db;

    public AssignmentService(IAppDbContext db) => _db = db;

    public async Task<PagedResult<AssignmentDto>> ListAllAsync(
        AssignmentListQuery query, CancellationToken cancellationToken = default)
    {
        return await ApplyFilters(_db.Assignments.AsNoTracking(), query)
            .OrderByDescending(a => a.CreatedAt)
            .Select(Projections.ToAssignmentDto)
            .ToPagedResultAsync(query, cancellationToken);
    }

    private static IQueryable<Assignment> ApplyFilters(
        IQueryable<Assignment> assignments, AssignmentListQuery query)
    {
        if (query.Status is not null)
        {
            assignments = assignments.Where(a => a.Status == query.Status);
        }

        if (query.ClassCourseId is not null)
        {
            assignments = assignments.Where(a => a.ClassCourseId == query.ClassCourseId);
        }

        if (query.SubjectId is not null)
        {
            assignments = assignments.Where(a => a.SubjectId == query.SubjectId);
        }

        if (query.TeacherId is not null)
        {
            assignments = assignments.Where(a => a.CreatedByTeacherId == query.TeacherId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            assignments = assignments.Where(a => a.Title.ToLower().Contains(term));
        }

        return assignments;
    }
}
