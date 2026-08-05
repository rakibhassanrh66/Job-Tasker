// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSystem.Api.Controllers;

[ApiController]
[Route("api/v1/meta")]
[Produces("application/json")]
public class MetaController : ControllerBase
{
    /// <summary>
    /// Build and authorship information.
    ///
    /// Anonymous on purpose — it states openly who wrote this and under what licence.
    /// Everything returned is a compile-time constant or an environment variable already
    /// present on the host; nothing is collected and nothing is sent anywhere.
    /// </summary>
    /// <response code="200">Build metadata.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MetaResponse), StatusCodes.Status200OK)]
    public ActionResult<MetaResponse> Get()
    {
        return Ok(new MetaResponse(
            Author: BuildInfo.Author,
            Contact: BuildInfo.Contact,
            Purpose: BuildInfo.Purpose,
            Canary: BuildInfo.Canary,
            Commit: Environment.GetEnvironmentVariable("GIT_SHA") ?? "local",
            BuiltUtc: DateTime.UtcNow));
    }
}

public record MetaResponse(
    string Author,
    string Contact,
    string Purpose,
    string Canary,
    string Commit,
    DateTime BuiltUtc);
