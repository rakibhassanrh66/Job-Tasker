// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Classes;
using AssignmentSystem.Application.Enrollments;
using AssignmentSystem.Application.Subjects;
using AssignmentSystem.Application.TeacherAssignments;
using FluentValidation;

namespace AssignmentSystem.Application.Common.Validators;

public class CreateClassCourseRequestValidator : AbstractValidator<CreateClassCourseRequest>
{
    public CreateClassCourseRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required.").MaximumLength(50);
    }
}

public class UpdateClassCourseRequestValidator : AbstractValidator<UpdateClassCourseRequest>
{
    public UpdateClassCourseRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required.").MaximumLength(50);
    }
}

public class CreateSubjectRequestValidator : AbstractValidator<CreateSubjectRequest>
{
    public CreateSubjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required.").MaximumLength(50);
        RuleFor(x => x.ClassCourseId).NotEmpty().WithMessage("A class must be specified.");
    }
}

public class UpdateSubjectRequestValidator : AbstractValidator<UpdateSubjectRequest>
{
    public UpdateSubjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required.").MaximumLength(50);
    }
}

public class CreateTeacherAssignmentRequestValidator : AbstractValidator<CreateTeacherAssignmentRequest>
{
    public CreateTeacherAssignmentRequestValidator()
    {
        RuleFor(x => x.TeacherId).NotEmpty().WithMessage("A teacher must be specified.");
        RuleFor(x => x.SubjectId).NotEmpty().WithMessage("A subject must be specified.");
        RuleFor(x => x.ClassCourseId).NotEmpty().WithMessage("A class must be specified.");
    }
}

public class CreateEnrollmentRequestValidator : AbstractValidator<CreateEnrollmentRequest>
{
    public CreateEnrollmentRequestValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty().WithMessage("A student must be specified.");
        RuleFor(x => x.ClassCourseId).NotEmpty().WithMessage("A class must be specified.");
    }
}
