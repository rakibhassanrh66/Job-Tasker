// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Auth;
using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Application.Common.Security;
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

        return services;
    }
}
