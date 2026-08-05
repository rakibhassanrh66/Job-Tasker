// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

namespace AssignmentSystem.Api.Middleware;

/// <summary>
/// Response hardening headers.
///
/// This host serves two very different things, so it sends two different policies. Every
/// API route returns JSON, which needs to load nothing at all — `default-src 'none'` is
/// exactly right there and is as strict as CSP goes. Swagger UI is a real HTML page with
/// its own scripts and styles, and Swashbuckle emits an inline bootstrap script, so the
/// same policy would leave an evaluator staring at a blank page.
///
/// The alternative — one relaxed policy everywhere — would mean weakening the API's
/// headers to accommodate a documentation page, which is backwards.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        var isSwagger = context.Request.Path.StartsWithSegments("/swagger");

        // On the starting callback, like BuildSignatureMiddleware: once a later middleware
        // begins writing the body the headers are already sent, and adding one throws.
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // Stops a browser second-guessing Content-Type. Without it, a JSON response
            // holding attacker-influenced text can be sniffed as HTML and executed.
            headers["X-Content-Type-Options"] = "nosniff";

            // Clickjacking. X-Frame-Options is the legacy header; frame-ancestors in the
            // CSP below is the modern one. Both are sent because older browsers only
            // understand the first and it costs nothing.
            headers["X-Frame-Options"] = "DENY";

            // Never leak a URL — which for this API contains resource ids — to another
            // origin through the Referer header.
            headers["Referrer-Policy"] = "no-referrer";

            headers["Content-Security-Policy"] = isSwagger
                ? "default-src 'self'; "
                  + "script-src 'self' 'unsafe-inline'; "
                  + "style-src 'self' 'unsafe-inline'; "
                  + "img-src 'self' data:; "
                  + "connect-src 'self'; "
                  + "frame-ancestors 'none'"
                : "default-src 'none'; frame-ancestors 'none'";

            return Task.CompletedTask;
        });

        return _next(context);
    }
}
