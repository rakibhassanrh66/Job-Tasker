// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

namespace AssignmentSystem.Domain.Exceptions;

/// <summary>
/// The submission window is closed: the assignment does not allow the attempted action, or
/// it is no longer the student's to make. Covers business rule 5 (late submit when
/// AllowLateSubmission is false) and business rule 7 (update after the deadline, when
/// AllowUpdateBeforeDeadline is false, or once a teacher has reviewed the work). Maps to 409.
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

    public static SubmissionClosedException AlreadyGraded() =>
        new("This submission has been reviewed and can no longer be changed.");

    public override string Title => "Submission closed";

    public override int StatusCode => 409;
}
