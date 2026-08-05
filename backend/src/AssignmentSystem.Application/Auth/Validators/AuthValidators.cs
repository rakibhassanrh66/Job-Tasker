// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Auth.Dtos;
using FluentValidation;

namespace AssignmentSystem.Application.Auth.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(256)
            .EmailAddress().WithMessage("Email must be a valid address.");

        // Only presence and an upper bound are checked here. Enforcing a minimum length or
        // complexity on *login* would reject a short legacy password outright and, more
        // usefully to an attacker, distinguish "malformed" from "wrong" before any lookup
        // happens. Strength rules belong on the create-user path instead.
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MaximumLength(256);
    }
}

public class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.")
            .MaximumLength(500);
    }
}
