// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

namespace AssignmentSystem.Domain.Enums;

/// <summary>
/// Lifecycle of a submission. <see cref="Late"/> is only ever set at creation time,
/// when a student submits after the deadline on an assignment that permits it
/// (business rule 5) — it is never transitioned into afterwards.
/// Values start at 1 so default(SubmissionStatus) is not Submitted.
/// </summary>
public enum SubmissionStatus
{
    Submitted = 1,
    UnderReview = 2,
    Graded = 3,
    Returned = 4,
    Late = 5
}
