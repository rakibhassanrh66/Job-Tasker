// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Submissions.Dtos;
using FluentValidation;

namespace AssignmentSystem.Application.Submissions.Validators;

/// <summary>
/// Note what is absent: any bound on Marks.
///
/// The real rule is 0 to the parent assignment's MaxMarks, and a validator cannot see the
/// parent. Adding a partial check here would split rule 9 across two places, and the
/// weaker copy would be the one people read. The service owns it entirely and returns 422,
/// the same status this validator would have.
/// </summary>
public class GradeSubmissionRequestValidator : AbstractValidator<GradeSubmissionRequest>
{
    public GradeSubmissionRequestValidator()
    {
        RuleFor(x => x.Feedback)
            .MaximumLength(5000)
            .When(x => x.Feedback is not null);
    }
}

public class ChangeSubmissionStatusRequestValidator : AbstractValidator<ChangeSubmissionStatusRequest>
{
    public ChangeSubmissionStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status is not a recognised submission status.");
    }
}

public class CreateSubmissionRequestValidator : AbstractValidator<CreateSubmissionRequest>
{
    public CreateSubmissionRequestValidator()
    {
        RuleFor(x => x.AnswerText)
            .NotEmpty().WithMessage("An answer is required.")
            .MaximumLength(10000);

        RuleFor(x => x.AttachmentUrl)
            .MaximumLength(1000)
            .When(x => x.AttachmentUrl is not null);
    }
}

public class UpdateSubmissionRequestValidator : AbstractValidator<UpdateSubmissionRequest>
{
    public UpdateSubmissionRequestValidator()
    {
        RuleFor(x => x.AnswerText)
            .NotEmpty().WithMessage("An answer is required.")
            .MaximumLength(10000);

        RuleFor(x => x.AttachmentUrl)
            .MaximumLength(1000)
            .When(x => x.AttachmentUrl is not null);
    }
}
