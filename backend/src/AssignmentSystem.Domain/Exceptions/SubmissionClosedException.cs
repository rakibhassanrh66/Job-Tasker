// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

namespace AssignmentSystem.Domain.Exceptions;

/// <summary>
/// The submission window is closed: the deadline has passed and the assignment does not
/// allow the attempted action. Covers business rule 5 (late submit when
/// AllowLateSubmission is false) and business rule 7 (update after the deadline, or when
/// AllowUpdateBeforeDeadline is false). Maps to 409.
/// </summary>
public sealed class SubmissionClosedException : DomainException
{
    public SubmissionClosedException(string message) : base(message)
    {
    }

    public static SubmissionClosedException DeadlinePassed() =>
        new("The deadline for this assignment has passed.");

    public static SubmissionClosedException UpdatesNotAllowed() =>
        new("This assignment does not allow submissions to be updated.");

    public override string Title => "Submission closed";

    public override int StatusCode => 409;
}
