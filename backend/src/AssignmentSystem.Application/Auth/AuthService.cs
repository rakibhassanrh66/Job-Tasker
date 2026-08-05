// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Auth.Dtos;
using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AssignmentSystem.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default);

    Task<UserProfile> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}

public class AuthService : IAuthService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IAppDbContext db,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ICurrentUser currentUser,
        IClock clock,
        ILogger<AuthService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = Normalise(request.Email);

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            // Verify against a throwaway hash anyway. Returning immediately here would make
            // "unknown email" measurably faster than "wrong password", which is enough to
            // enumerate accounts by timing alone.
            _passwordHasher.Verify(DummyHash, request.Password);

            _logger.LogWarning("Login rejected: no account for {Email}.", email);
            throw new InvalidCredentialsException();
        }

        if (!_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            _logger.LogWarning("Login rejected: bad password for {Email}.", email);
            throw new InvalidCredentialsException();
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login rejected: account {Email} is deactivated.", email);
            throw new InvalidCredentialsException();
        }

        _logger.LogInformation("Login succeeded for {Email} ({Role}).", email, user.Role);

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(
        RefreshRequest request, CancellationToken cancellationToken = default)
    {
        var hash = _tokenService.HashRefreshToken(request.RefreshToken);

        var stored = await _db.RefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (stored is null)
        {
            _logger.LogWarning("Refresh rejected: token not recognised.");
            throw InvalidCredentialsException.InvalidRefreshToken();
        }

        if (stored.RevokedAt is not null)
        {
            // A revoked token being presented means the client is replaying an old value,
            // which most plausibly means it leaked. The safe response is to assume the
            // whole chain is compromised and force a fresh login.
            _logger.LogWarning(
                "Refresh rejected: revoked token replayed for user {UserId}. Revoking all active tokens.",
                stored.UserId);

            await RevokeAllActiveTokensAsync(stored.UserId, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            throw InvalidCredentialsException.InvalidRefreshToken();
        }

        if (!stored.IsActive(_clock.UtcNow))
        {
            _logger.LogWarning("Refresh rejected: token expired for user {UserId}.", stored.UserId);
            throw InvalidCredentialsException.InvalidRefreshToken();
        }

        if (!stored.User.IsActive)
        {
            _logger.LogWarning("Refresh rejected: user {UserId} is deactivated.", stored.UserId);
            throw InvalidCredentialsException.InvalidRefreshToken();
        }

        var response = await IssueTokensAsync(stored.User, cancellationToken, rotatedFrom: stored);

        return response;
    }

    public async Task<UserProfile> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.RequireUserId();

        var user = await _db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);

        return UserProfile.From(user);
    }

    // ---------------------------------------------------------------------------------

    private async Task<AuthResponse> IssueTokensAsync(
        User user,
        CancellationToken cancellationToken,
        RefreshToken? rotatedFrom = null)
    {
        var now = _clock.UtcNow;
        var access = _tokenService.CreateAccessToken(user);
        var (rawRefresh, refreshHash) = _tokenService.CreateRefreshToken();

        var refresh = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = now.Add(_tokenService.RefreshTokenLifetime),
            CreatedAt = now
        };

        _db.RefreshTokens.Add(refresh);

        if (rotatedFrom is not null)
        {
            // Rotation: the presented token dies as the new one is born, and the link
            // between them makes a replay chain traceable.
            rotatedFrom.RevokedAt = now;
            rotatedFrom.ReplacedByTokenId = refresh.Id;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            access.Value,
            access.ExpiresAtUtc,
            rawRefresh,
            refresh.ExpiresAt,
            UserProfile.From(user));
    }

    private async Task RevokeAllActiveTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        var active = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in active)
        {
            token.RevokedAt = now;
        }
    }

    private static string Normalise(string email) => email.Trim().ToLowerInvariant();

    /// <summary>A real PasswordHasher output, used only to spend comparable time when the
    /// account does not exist. Corresponds to no usable password.</summary>
    private const string DummyHash =
        "AQAAAAIAAYagAAAAEK7Vv0mQ0mL7bVQ1yQxJ7YdT8kK1cQ2Vv0mQ0mL7bVQ1yQxJ7YdT8kK1cQ2Vv0mQ0m==";
}
