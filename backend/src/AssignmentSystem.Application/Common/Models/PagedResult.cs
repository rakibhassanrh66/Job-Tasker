// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.Application.Common.Models;

/// <summary>Page of results plus the metadata a client needs to render pagination.</summary>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;
}

/// <summary>
/// Page and size for a list request.
///
/// Both are clamped rather than validated into an error. A caller asking for page 0 or
/// 10,000 items almost certainly wants the nearest sensible page, and an unbounded page
/// size is a denial-of-service waiting to happen — one request could ask the database for
/// every row in the table.
/// </summary>
public class PagedQuery
{
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 20;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }

    public int Skip => (Page - 1) * PageSize;
}

public static class QueryablePagingExtensions
{
    /// <summary>
    /// Counts and pages in two queries against the database.
    ///
    /// The projection is applied before the page is taken so only the selected columns are
    /// read, and the count runs on the filtered query rather than in memory — the whole
    /// point being that a page of twenty never loads a table of twenty thousand.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PagedQuery paging,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, paging.Page, paging.PageSize, totalCount);
    }
}
