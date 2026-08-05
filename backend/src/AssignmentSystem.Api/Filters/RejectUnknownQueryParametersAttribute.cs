// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AssignmentSystem.Api.Filters;

/// <summary>
/// Rejects query-string keys the action cannot bind, with 400.
///
/// Model binding ignores anything it does not recognise, so without this an endpoint
/// answers 200 to a filter it silently discarded — <c>?status=Draft</c> on a route that
/// has no status filter returns published rows and looks like it worked. Splitting the
/// query types stops Swagger advertising those parameters; this stops the API accepting
/// them anyway.
///
/// 400 rather than 422 follows the same line the ValidationFilter draws: 422 means a
/// request that was understood and broke a rule, and a parameter this endpoint does not
/// define was never understood in the first place.
///
/// Applied per action rather than globally, so it governs the list endpoints where
/// filters are the whole interface and leaves everything else alone.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RejectUnknownQueryParametersAttribute : ActionFilterAttribute
{
    // Reflection once per action, not once per request.
    private static readonly ConcurrentDictionary<string, HashSet<string>> Recognised = new();

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var recognised = Recognised.GetOrAdd(
            context.ActionDescriptor.Id, _ => Bindable(context.ActionDescriptor));

        var unknown = context.HttpContext.Request.Query.Keys
            .Where(key => !recognised.Contains(key))
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (unknown.Length == 0)
        {
            base.OnActionExecuting(context);
            return;
        }

        var problem = new ProblemDetails
        {
            Type = "about:blank",
            Title = "Unknown query parameter",
            Status = StatusCodes.Status400BadRequest,
            Detail = unknown.Length == 1
                ? $"'{unknown[0]}' is not a recognised parameter on this endpoint."
                : $"{string.Join(", ", unknown.Select(u => $"'{u}'"))} are not recognised "
                  + "parameters on this endpoint.",
            Instance = context.HttpContext.Request.Path
        };

        problem.Extensions["traceId"] =
            Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
        problem.Extensions["unknownParameters"] = unknown;

        context.Result = new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentTypes = { "application/problem+json" }
        };
    }

    /// <summary>
    /// Every name this action could bind from the query string: simple parameters by their
    /// own name, complex ones by their settable properties — which is how a [FromQuery]
    /// model binds, and which picks up Page and PageSize from PagedQuery for free.
    /// </summary>
    private static HashSet<string> Bindable(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor descriptor)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var parameters = descriptor is ControllerActionDescriptor controller
            ? controller.MethodInfo.GetParameters().Select(p => (p.Name, p.ParameterType))
            : descriptor.Parameters.Select(p => ((string?)p.Name, p.ParameterType));

        foreach (var (name, type) in parameters)
        {
            if (type == typeof(CancellationToken))
            {
                continue;
            }

            if (IsSimple(type))
            {
                if (name is not null)
                {
                    names.Add(name);
                }

                continue;
            }

            foreach (var property in type.GetProperties(
                         BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.CanWrite)
                {
                    names.Add(property.Name);
                }
            }
        }

        return names;
    }

    private static bool IsSimple(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        return underlying.IsPrimitive
               || underlying.IsEnum
               || underlying == typeof(string)
               || underlying == typeof(Guid)
               || underlying == typeof(decimal)
               || underlying == typeof(DateTime)
               || underlying == typeof(DateTimeOffset)
               || underlying == typeof(TimeSpan);
    }
}
