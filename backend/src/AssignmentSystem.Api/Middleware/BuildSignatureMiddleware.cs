// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Infrastructure;

namespace AssignmentSystem.Api.Middleware;

/// <summary>
/// Stamps authorship headers on every response.
///
/// Openly declared attribution, not a covert channel: the values are constants compiled
/// into the assembly, visible to anyone who looks at a response, and nothing is
/// transmitted anywhere. See LICENSE.
/// </summary>
public class BuildSignatureMiddleware
{
    public const string BuiltByHeader = "X-Built-By";
    public const string CanaryHeader = "X-Canary";

    private readonly RequestDelegate _next;

    public BuildSignatureMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        // Set on the starting callback rather than inline: by the time a later middleware
        // begins writing the body, headers are already sent and adding one would throw.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[BuiltByHeader] =
                $"{BuildInfo.Author} (evaluation build, see LICENSE)";
            context.Response.Headers[CanaryHeader] = BuildInfo.Canary;

            return Task.CompletedTask;
        });

        return _next(context);
    }
}
