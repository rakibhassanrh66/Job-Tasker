// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Domain.Enums;
using AssignmentSystem.Domain.Exceptions;

namespace AssignmentSystem.Application.Submissions;

/// <summary>
/// Business rule 10 — the submission lifecycle, in one place and testable as a pure
/// function over (from, to).
/// </summary>
public static class SubmissionStatusPolicy
{
    /// <summary>
    /// Explicit transition table.
    ///
    /// Late behaves exactly like Submitted from here on: it is set once, at creation, when
    /// a student submits after the deadline on an assignment that allows it (rule 5). It
    /// is never transitioned *into*, which is why nothing maps to it.
    /// </summary>
    public static bool CanTransition(SubmissionStatus from, SubmissionStatus to) =>
        (from, to) switch
        {
            (SubmissionStatus.Submitted, SubmissionStatus.UnderReview) => true,
            (SubmissionStatus.Submitted, SubmissionStatus.Graded) => true,
            (SubmissionStatus.Submitted, SubmissionStatus.Returned) => true,

            (SubmissionStatus.Late, SubmissionStatus.UnderReview) => true,
            (SubmissionStatus.Late, SubmissionStatus.Graded) => true,
            (SubmissionStatus.Late, SubmissionStatus.Returned) => true,

            (SubmissionStatus.UnderReview, SubmissionStatus.Graded) => true,
            (SubmissionStatus.UnderReview, SubmissionStatus.Returned) => true,

            (SubmissionStatus.Graded, SubmissionStatus.Returned) => true,

            _ => false
        };

    public static void EnsureCanTransition(SubmissionStatus from, SubmissionStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidStatusTransitionException(from, to);
        }
    }

    /// <summary>
    /// Whether a submission in this state can be graded.
    ///
    /// Grading is allowed directly from Submitted or Late without first passing through
    /// UnderReview: the act of entering marks *is* the review, and requiring two calls to
    /// record one decision would be ceremony. The explicit status endpoint still enforces
    /// the full chain, so a teacher who wants to mark work as under review can.
    /// </summary>
    public static bool CanGrade(SubmissionStatus current) =>
        current is SubmissionStatus.Submitted
            or SubmissionStatus.Late
            or SubmissionStatus.UnderReview;

    public static void EnsureCanGrade(SubmissionStatus current)
    {
        if (!CanGrade(current))
        {
            throw new InvalidStatusTransitionException(current, SubmissionStatus.Graded);
        }
    }
}
