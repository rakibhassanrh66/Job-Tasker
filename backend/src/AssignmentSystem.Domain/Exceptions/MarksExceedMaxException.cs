// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

namespace AssignmentSystem.Domain.Exceptions;

/// <summary>
/// Marks fall outside [0, Assignment.MaxMarks] (business rule 9). Maps to 422.
///
/// The lower bound is additionally enforced by a database CHECK constraint. The upper
/// bound cannot be, because a PostgreSQL CHECK cannot reference MaxMarks on the parent
/// Assignment row — so this exception is the authoritative guard for it. See docs/ERD.md.
/// </summary>
public sealed class MarksExceedMaxException : DomainException
{
    public MarksExceedMaxException(int marks, int maxMarks)
        : base($"Marks must be between 0 and {maxMarks}. Received {marks}.")
    {
        Marks = marks;
        MaxMarks = maxMarks;
    }

    public int Marks { get; }

    public int MaxMarks { get; }

    public override string Title => "Marks out of range";

    public override int StatusCode => 422;
}
