// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AssignmentSystem.Api.Filters;

/// <summary>
/// Runs the FluentValidation validator for each action argument and returns 422 with
/// per-field errors when one fails.
///
/// 422 rather than 400 is deliberate and matches the API contract: 400 means the request
/// could not be understood — malformed JSON, a Guid where an int was expected — while 422
/// means it was understood perfectly and broke a rule. A client can act on that
/// difference; retrying is pointless for one and meaningful for the other.
/// </summary>
public class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _services;

    public ValidationFilter(IServiceProvider services) => _services = services;

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var errors = new Dictionary<string, List<string>>();

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());

            if (_services.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var result = await validator.ValidateAsync(
                new ValidationContext<object>(argument), context.HttpContext.RequestAborted);

            if (result.IsValid)
            {
                continue;
            }

            foreach (var failure in result.Errors)
            {
                if (!errors.TryGetValue(failure.PropertyName, out var messages))
                {
                    messages = new List<string>();
                    errors[failure.PropertyName] = messages;
                }

                messages.Add(failure.ErrorMessage);
            }
        }

        if (errors.Count > 0)
        {
            var problem = new ValidationProblemDetails(
                errors.ToDictionary(e => e.Key, e => e.Value.ToArray()))
            {
                Type = "about:blank",
                Title = "Validation failed",
                Status = StatusCodes.Status422UnprocessableEntity,
                Instance = context.HttpContext.Request.Path
            };

            problem.Extensions["traceId"] =
                Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

            context.Result = new ObjectResult(problem)
            {
                StatusCode = StatusCodes.Status422UnprocessableEntity,
                ContentTypes = { "application/problem+json" }
            };

            return;
        }

        await next();
    }
}
