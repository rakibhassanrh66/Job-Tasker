// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Domain.Enums;
using AssignmentSystem.Domain.Exceptions;

namespace AssignmentSystem.Application.Assignments;

/// <summary>
/// Business rule 11 — the assignment lifecycle, in one place.
///
/// Written as a pure function over (from, to) so the whole rule can be tested exhaustively
/// without a database, a teacher, or an HTTP request. Every service that changes an
/// assignment's status routes through here, so there is exactly one definition of what is
/// allowed rather than an `if` at each call site that can drift.
/// </summary>
public static class AssignmentStatusPolicy
{
    public static bool CanTransition(AssignmentStatus from, AssignmentStatus to) =>
        (from, to) switch
        {
            (AssignmentStatus.Draft, AssignmentStatus.Published) => true,
            (AssignmentStatus.Published, AssignmentStatus.Archived) => true,
            (AssignmentStatus.Draft, AssignmentStatus.Archived) => true,
            _ => false
        };

    /// <summary>Throws <see cref="InvalidStatusTransitionException"/> (409) when the move is
    /// not permitted. Re-publishing an already Published assignment lands here rather than
    /// silently succeeding, so the caller learns nothing happened.</summary>
    public static void EnsureCanTransition(AssignmentStatus from, AssignmentStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidStatusTransitionException(from, to);
        }
    }

    /// <summary>Only a Draft may be published. An assignment that is already Published, or
    /// has been Archived, is rejected.</summary>
    public static void EnsureCanPublish(AssignmentStatus current) =>
        EnsureCanTransition(current, AssignmentStatus.Published);

    /// <summary>
    /// Whether an assignment is visible to students. Draft and Archived are not
    /// (business rule 1).
    /// </summary>
    public static bool IsVisibleToStudents(AssignmentStatus status) =>
        status == AssignmentStatus.Published;
}
