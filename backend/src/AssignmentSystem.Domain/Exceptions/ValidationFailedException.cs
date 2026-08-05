// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

namespace AssignmentSystem.Domain.Exceptions;

/// <summary>
/// A request that is well-formed but semantically wrong. Maps to 422.
///
/// Distinct from the FluentValidation filter, which can only judge a request in isolation —
/// "email must look like an email". This covers the rules that need the database to
/// evaluate: that a subject actually belongs to the class it is being paired with, or that
/// the user being allocated as a teacher really holds the Teacher role. Same status code,
/// because from the caller's side both mean "understood, but not acceptable".
/// </summary>
public sealed class ValidationFailedException : DomainException
{
    public ValidationFailedException(string field, string message) : base(message)
    {
        Field = field;
    }

    /// <summary>The offending field, so the response can point at it the same way the
    /// validation filter does.</summary>
    public string Field { get; }

    public override string Title => "Validation failed";

    public override int StatusCode => 422;
}
