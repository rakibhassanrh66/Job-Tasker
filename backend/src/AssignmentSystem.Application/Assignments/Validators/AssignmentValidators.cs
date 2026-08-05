// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Assignments.Dtos;
using AssignmentSystem.Application.Common.Interfaces;
using FluentValidation;

namespace AssignmentSystem.Application.Assignments.Validators;

public class CreateAssignmentRequestValidator : AbstractValidator<CreateAssignmentRequest>
{
    public CreateAssignmentRequestValidator(IClock clock)
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(300);

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(5000);

        // Creating an assignment whose deadline has already passed would produce something
        // no student could ever submit to.
        RuleFor(x => x.Deadline)
            .GreaterThan(_ => clock.UtcNow)
            .WithMessage("Deadline must be in the future.");

        RuleFor(x => x.MaxMarks)
            .GreaterThan(0).WithMessage("Maximum marks must be greater than zero.")
            .LessThanOrEqualTo(1000).WithMessage("Maximum marks must be 1000 or less.");

        RuleFor(x => x.ClassCourseId).NotEmpty().WithMessage("A class must be specified.");
        RuleFor(x => x.SubjectId).NotEmpty().WithMessage("A subject must be specified.");
    }
}

public class UpdateAssignmentRequestValidator : AbstractValidator<UpdateAssignmentRequest>
{
    public UpdateAssignmentRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(300);

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(5000);

        // Unlike create, the deadline is not required to be in the future. Moving it into
        // the past is how a teacher closes an assignment early, which is a legitimate act
        // rather than a mistake.
        RuleFor(x => x.MaxMarks)
            .GreaterThan(0).WithMessage("Maximum marks must be greater than zero.")
            .LessThanOrEqualTo(1000).WithMessage("Maximum marks must be 1000 or less.");
    }
}
