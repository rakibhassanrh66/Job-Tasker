// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

namespace AssignmentSystem.Domain.Exceptions;

/// <summary>
/// The caller is authenticated and holds the right role, but does not own — or is not
/// scoped to — this particular resource. Maps to 403.
///
/// This is the ownership half of authorization (business rules 3, 4, 8): being a Teacher
/// is not sufficient to grade a submission; it must belong to an assignment that teacher
/// created. Deliberately carries no detail about the resource, so a probing caller cannot
/// use 403-vs-404 to discover which ids exist.
/// </summary>
public sealed class ForbiddenResourceException : DomainException
{
    public ForbiddenResourceException(string message) : base(message)
    {
    }

    public override string Title => "Forbidden";

    public override int StatusCode => 403;
}
