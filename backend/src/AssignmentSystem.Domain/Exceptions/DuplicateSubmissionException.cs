// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

namespace AssignmentSystem.Domain.Exceptions;

/// <summary>
/// A student already has a submission for this assignment (business rule 6). One
/// submission per student per assignment; content changes go through the update
/// endpoint instead of creating a second row. Maps to 409.
///
/// The service checks for this before inserting, but a unique index on
/// (AssignmentId, StudentId) is the real guarantee — it also closes the race where
/// two concurrent requests both pass the check.
/// </summary>
public sealed class DuplicateSubmissionException : DomainException
{
    public DuplicateSubmissionException()
        : base("A submission already exists for this assignment. Update it instead.")
    {
    }

    public override string Title => "Submission already exists";

    public override int StatusCode => 409;
}
