// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Common;
using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Application.Submissions.Dtos;
using AssignmentSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.Application.Submissions;

public partial interface ISubmissionService
{
    /// <summary>Unfiltered listing for administrative oversight. See the note on the
    /// equivalent assignment method — scoped listings are separate methods.</summary>
    Task<PagedResult<SubmissionDto>> ListAllAsync(
        SubmissionListQuery query, CancellationToken cancellationToken = default);
}

public partial class SubmissionService : ISubmissionService
{
    private readonly IAppDbContext _db;

    public SubmissionService(IAppDbContext db) => _db = db;

    public async Task<PagedResult<SubmissionDto>> ListAllAsync(
        SubmissionListQuery query, CancellationToken cancellationToken = default)
    {
        return await ApplyFilters(_db.Submissions.AsNoTracking(), query)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(Projections.ToSubmissionDto)
            .ToPagedResultAsync(query, cancellationToken);
    }

    private static IQueryable<Submission> ApplyFilters(
        IQueryable<Submission> submissions, SubmissionListQuery query)
    {
        if (query.AssignmentId is not null)
        {
            submissions = submissions.Where(s => s.AssignmentId == query.AssignmentId);
        }

        if (query.StudentId is not null)
        {
            submissions = submissions.Where(s => s.StudentId == query.StudentId);
        }

        if (query.Status is not null)
        {
            submissions = submissions.Where(s => s.Status == query.Status);
        }

        return submissions;
    }
}
