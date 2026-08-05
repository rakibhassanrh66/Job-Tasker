// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using System.Diagnostics;
using AssignmentSystem.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSystem.Api.Middleware;

/// <summary>
/// Translates domain exceptions into ProblemDetails responses.
///
/// Because this exists, services can express a rule violation by throwing the exception
/// that names it, and controllers never map status codes by hand. Each
/// <see cref="DomainException"/> carries its own status and title, so adding a rule does
/// not mean editing a switch statement here.
/// </summary>
public class ExceptionHandlingMiddleware
{
    /// <summary>RFC 7807 media type for structured error bodies.</summary>
    public const string ProblemContentType = "application/problem+json";

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            await WriteDomainExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            await WriteUnhandledExceptionAsync(context, ex);
        }
    }

    private async Task WriteDomainExceptionAsync(HttpContext context, DomainException exception)
    {
        // Every rule rejection is logged at Warning: these are the events worth reviewing
        // when someone reports "it said 403 and I don't know why".
        _logger.LogWarning(
            "Rule rejected {Method} {Path} with {Status}: {Message}",
            context.Request.Method,
            context.Request.Path,
            exception.StatusCode,
            exception.Message);

        var problem = new ProblemDetails
        {
            Type = "about:blank",
            Title = exception.Title,
            Status = exception.StatusCode,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        await WriteAsync(context, problem);
    }

    private async Task WriteUnhandledExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(
            exception,
            "Unhandled exception for {Method} {Path}",
            context.Request.Method,
            context.Request.Path);

        var problem = new ProblemDetails
        {
            Type = "about:blank",
            Title = "An unexpected error occurred",
            Status = StatusCodes.Status500InternalServerError,
            Instance = context.Request.Path,

            // Outside Development the message is generic. Exception text routinely contains
            // table names, file paths and connection details, none of which belong in a
            // response to a caller who just triggered a bug.
            Detail = _environment.IsDevelopment()
                ? exception.ToString()
                : "An unexpected error occurred. Please contact support with the traceId."
        };

        await WriteAsync(context, problem);
    }

    private static async Task WriteAsync(HttpContext context, ProblemDetails problem)
    {
        if (context.Response.HasStarted)
        {
            // Headers are already on the wire; rewriting the status now would corrupt the
            // response. Nothing useful is left to do but let it fail.
            return;
        }

        problem.Extensions["traceId"] =
            Activity.Current?.Id ?? context.TraceIdentifier;

        context.Response.Clear();
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        // The content type is passed to WriteAsJsonAsync rather than assigned beforehand:
        // that method sets application/json itself and would overwrite it. RFC 7807 asks
        // for application/problem+json, which is how a client tells a structured error
        // apart from an ordinary JSON payload.
        await context.Response.WriteAsJsonAsync(
            problem, options: null, contentType: ProblemContentType);
    }
}
