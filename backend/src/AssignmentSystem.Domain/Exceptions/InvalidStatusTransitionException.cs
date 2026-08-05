// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

namespace AssignmentSystem.Domain.Exceptions;

/// <summary>
/// An attempt to move an entity between two states the lifecycle does not permit —
/// business rule 10 for submissions, business rule 11 for assignment publishing
/// (re-publishing an already Published assignment lands here). Maps to 409.
/// </summary>
public sealed class InvalidStatusTransitionException : DomainException
{
    public InvalidStatusTransitionException(string message) : base(message)
    {
    }

    public InvalidStatusTransitionException(object from, object to)
        : base($"Cannot transition from '{from}' to '{to}'.")
    {
    }

    public override string Title => "Invalid status transition";

    public override int StatusCode => 409;
}
