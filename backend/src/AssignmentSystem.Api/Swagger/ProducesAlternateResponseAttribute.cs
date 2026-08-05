// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

namespace AssignmentSystem.Api.Swagger;

/// <summary>
/// Declares that one status code can carry more than one body shape.
///
/// [ProducesResponseType] allows a single type per status, so an action that answers
/// differently depending on the caller's role can only ever document one of its shapes —
/// and adding a second attribute for the same status has Swashbuckle pick one arbitrarily.
/// <see cref="AlternateResponseOperationFilter"/> turns this into a proper <c>oneOf</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ProducesAlternateResponseAttribute : Attribute
{
    public ProducesAlternateResponseAttribute(int statusCode, params Type[] types)
    {
        StatusCode = statusCode;
        Types = types;
    }

    public int StatusCode { get; }

    public IReadOnlyList<Type> Types { get; }
}
