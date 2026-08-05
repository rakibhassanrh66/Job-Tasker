// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

namespace AssignmentSystem.Api.Configuration;

/// <summary>
/// Loads a .env file into environment variables for local development.
///
/// Under docker compose these values already arrive as real environment variables, so
/// this is a no-op there. It exists so that running the API directly with `dotnet run`
/// reads the same .env file rather than needing a second, divergent config source —
/// which is how connection strings and signing keys end up committed by accident.
///
/// Existing environment variables always win, so a real deployment cannot be overridden
/// by a stray file.
/// </summary>
public static class DotEnvLoader
{
    public static void Load(string startDirectory)
    {
        var path = FindUpwards(startDirectory, ".env");

        if (path is null)
        {
            return;
        }

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separator = line.IndexOf('=');

            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim().Trim('"');

            if (Environment.GetEnvironmentVariable(key) is null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    /// <summary>Walks up from the start directory looking for the file, so the API can be
    /// launched from either the repo root or its own project folder.</summary>
    private static string? FindUpwards(string startDirectory, string fileName)
    {
        var directory = new DirectoryInfo(startDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, fileName);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
