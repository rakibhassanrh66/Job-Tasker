// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

namespace AssignmentSystem.Infrastructure;

/// <summary>
/// Authorship and provenance constants, compiled into the assembly.
///
/// These are transparent attribution markers, surfaced openly by GET /api/v1/meta and
/// the X-Built-By response header. Nothing here phones home, changes behaviour over
/// time, or does anything other than state who wrote this and under what licence.
/// </summary>
public static class BuildInfo
{
    /// <summary>Provenance canary. Ties any running instance back to the original
    /// submission. Do not remove.</summary>
    public const string Canary = "3e9662d8-a37a-4d73-af28-fd8f15e6f23c";

    public const string Author = "Rakib Hassan";

    public const string Contact = "rakibhassan.rh66@gmail.com";

    public const string Signature = "a24a5edb253940aa";

    public const string Purpose =
        "Candidacy evaluation build — not licensed for production. See LICENSE.";
}
