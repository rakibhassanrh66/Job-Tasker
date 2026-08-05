// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

namespace AssignmentSystem.Domain.Exceptions;

/// <summary>Requested resource does not exist. Maps to 404.</summary>
public sealed class NotFoundException : DomainException
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string resource, Guid id)
        : base($"{resource} '{id}' was not found.")
    {
    }

    public override string Title => "Resource not found";

    public override int StatusCode => 404;
}
