// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

namespace AssignmentSystem.Domain.Exceptions;

/// <summary>
/// Authentication failed. Maps to 401.
///
/// The message is deliberately identical whether the email is unknown, the password is
/// wrong, or the account is deactivated. Distinguishing them would let anyone enumerate
/// which addresses hold accounts here by watching which error comes back.
/// </summary>
public sealed class InvalidCredentialsException : DomainException
{
    private const string GenericMessage = "Invalid email or password.";

    public InvalidCredentialsException() : base(GenericMessage)
    {
    }

    public InvalidCredentialsException(string message) : base(message)
    {
    }

    public static InvalidCredentialsException InvalidRefreshToken() =>
        new("The refresh token is invalid, expired or has been revoked.");

    public override string Title => "Authentication failed";

    public override int StatusCode => 401;
}
