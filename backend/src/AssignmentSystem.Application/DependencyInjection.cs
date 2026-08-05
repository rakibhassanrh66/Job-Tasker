// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Assignments;
using AssignmentSystem.Application.Auth;
using AssignmentSystem.Application.Classes;
using AssignmentSystem.Application.Submissions;
using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Application.Common.Security;
using AssignmentSystem.Application.Enrollments;
using AssignmentSystem.Application.Subjects;
using AssignmentSystem.Application.TeacherAssignments;
using AssignmentSystem.Application.Users;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AssignmentSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IResourceAuthorizer, ResourceAuthorizer>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IClassCourseService, ClassCourseService>();
        services.AddScoped<ISubjectService, SubjectService>();
        services.AddScoped<ITeacherAssignmentService, TeacherAssignmentService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddScoped<ISubmissionService, SubmissionService>();

        return services;
    }
}
