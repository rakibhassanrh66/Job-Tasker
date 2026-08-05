// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using System.IdentityModel.Tokens.Jwt;
using AssignmentSystem.Application.Auth;
using AssignmentSystem.Application.Auth.Dtos;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Enums;
using AssignmentSystem.Domain.Exceptions;
using AssignmentSystem.Infrastructure.Auth;
using AssignmentSystem.Infrastructure.Persistence;
using AssignmentSystem.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AssignmentSystem.UnitTests.Auth;

public class AuthServiceTests
{
    private static readonly DateTime Now = new(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc);

    private const string CorrectPassword = "Correct@123";

    private readonly TestClock _clock = new(Now);
    private readonly TestCurrentUser _currentUser = new();
    private readonly PasswordHasherAdapter _hasher = new();
    private readonly AppDbContext _db = TestDb.Create();

    private AuthService CreateService()
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            Key = "unit-test-signing-key-that-is-definitely-long-enough-32+",
            Issuer = "AssignmentSystem.Api",
            Audience = "AssignmentSystem.Client",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        });

        return new AuthService(
            _db,
            _hasher,
            new JwtTokenService(jwtOptions, _clock),
            _currentUser,
            _clock,
            NullLogger<AuthService>.Instance);
    }

    private async Task<User> SeedUserAsync(
        UserRole role = UserRole.Student,
        bool isActive = true,
        string email = "person@demo.test")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FullName = "Test Person",
            Role = role,
            IsActive = isActive,
            CreatedAt = Now,
            PasswordHash = _hasher.Hash(CorrectPassword)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return user;
    }

    // ---------------------------------------------------------------------------------
    // Login
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task Login_With_Valid_Credentials_Returns_Tokens()
    {
        var user = await SeedUserAsync(UserRole.Teacher);
        var service = CreateService();

        var result = await service.LoginAsync(new LoginRequest(user.Email, CorrectPassword));

        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.User.Email.Should().Be(user.Email);
        result.User.Role.Should().Be(UserRole.Teacher);
        result.AccessTokenExpiresAtUtc.Should().Be(Now.AddMinutes(15));
        result.RefreshTokenExpiresAtUtc.Should().Be(Now.AddDays(7));
    }

    [Fact]
    public async Task Login_Stores_Only_A_Hash_Of_The_Refresh_Token()
    {
        var user = await SeedUserAsync();
        var service = CreateService();

        var result = await service.LoginAsync(new LoginRequest(user.Email, CorrectPassword));

        var stored = await _db.RefreshTokens.SingleAsync();

        stored.TokenHash.Should().NotBe(result.RefreshToken,
            "a database leak must not hand over usable refresh tokens");
    }

    [Fact]
    public async Task Login_With_Wrong_Password_Fails()
    {
        var user = await SeedUserAsync();
        var service = CreateService();

        var act = async () => await service.LoginAsync(
            new LoginRequest(user.Email, "Wrong@123"));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Login_With_Unknown_Email_Fails()
    {
        var service = CreateService();

        var act = async () => await service.LoginAsync(
            new LoginRequest("nobody@demo.test", CorrectPassword));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Login_With_Inactive_User_Fails()
    {
        var user = await SeedUserAsync(isActive: false);
        var service = CreateService();

        var act = async () => await service.LoginAsync(
            new LoginRequest(user.Email, CorrectPassword));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Login_Failure_Messages_Are_Identical_For_Unknown_Email_And_Wrong_Password()
    {
        // Distinguishable messages would let anyone test which addresses hold accounts.
        var user = await SeedUserAsync();
        var service = CreateService();

        var unknown = await Record.ExceptionAsync(() =>
            service.LoginAsync(new LoginRequest("nobody@demo.test", CorrectPassword)));

        var wrongPassword = await Record.ExceptionAsync(() =>
            service.LoginAsync(new LoginRequest(user.Email, "Wrong@123")));

        unknown!.Message.Should().Be(wrongPassword!.Message);
    }

    [Fact]
    public async Task Login_Is_Case_Insensitive_On_Email()
    {
        var user = await SeedUserAsync(email: "person@demo.test");
        var service = CreateService();

        var result = await service.LoginAsync(
            new LoginRequest("  PERSON@Demo.TEST  ", CorrectPassword));

        result.User.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task Access_Token_Contains_Role_And_UserId_Claims()
    {
        var user = await SeedUserAsync(UserRole.Admin);
        var service = CreateService();

        var result = await service.LoginAsync(new LoginRequest(user.Email, CorrectPassword));

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);

        token.Claims.Should().Contain(c =>
            c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());

        token.Claims.Should().Contain(c =>
            c.Type == JwtTokenService.RoleClaimType && c.Value == nameof(UserRole.Admin));

        token.Claims.Should().Contain(c =>
            c.Type == JwtTokenService.UserIdClaimType && c.Value == user.Id.ToString());

        token.Claims.Should().Contain(c =>
            c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);

        token.Claims.Should().NotContain(c => c.Value == user.PasswordHash,
            "the password hash must never travel in a token");
    }

    // ---------------------------------------------------------------------------------
    // Refresh
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task Refresh_Rotates_And_Revokes_Old_Token()
    {
        var user = await SeedUserAsync();
        var service = CreateService();

        var login = await service.LoginAsync(new LoginRequest(user.Email, CorrectPassword));

        _clock.Advance(TimeSpan.FromMinutes(1));

        var refreshed = await service.RefreshAsync(new RefreshRequest(login.RefreshToken));

        refreshed.RefreshToken.Should().NotBe(login.RefreshToken, "the token must rotate");

        var tokens = await _db.RefreshTokens.OrderBy(t => t.CreatedAt).ToListAsync();
        tokens.Should().HaveCount(2);

        var original = tokens[0];
        var replacement = tokens[1];

        original.RevokedAt.Should().NotBeNull("the presented token dies on rotation");
        original.ReplacedByTokenId.Should().Be(replacement.Id,
            "the chain must be traceable if a revoked token is later replayed");
        replacement.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task Reused_Revoked_Refresh_Token_Is_Rejected()
    {
        var user = await SeedUserAsync();
        var service = CreateService();

        var login = await service.LoginAsync(new LoginRequest(user.Email, CorrectPassword));
        await service.RefreshAsync(new RefreshRequest(login.RefreshToken));

        // Present the already-rotated token a second time.
        var act = async () => await service.RefreshAsync(new RefreshRequest(login.RefreshToken));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Replaying_A_Revoked_Token_Revokes_The_Whole_Chain()
    {
        // Replay most plausibly means the token leaked, so the safe response is to assume
        // every outstanding token for that user is compromised.
        var user = await SeedUserAsync();
        var service = CreateService();

        var login = await service.LoginAsync(new LoginRequest(user.Email, CorrectPassword));
        var second = await service.RefreshAsync(new RefreshRequest(login.RefreshToken));

        await Record.ExceptionAsync(() =>
            service.RefreshAsync(new RefreshRequest(login.RefreshToken)));

        // The still-valid token issued by the legitimate rotation is now revoked too.
        var act = async () => await service.RefreshAsync(new RefreshRequest(second.RefreshToken));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Refresh_With_Expired_Token_Is_Rejected()
    {
        var user = await SeedUserAsync();
        var service = CreateService();

        var login = await service.LoginAsync(new LoginRequest(user.Email, CorrectPassword));

        _clock.Advance(TimeSpan.FromDays(8));

        var act = async () => await service.RefreshAsync(new RefreshRequest(login.RefreshToken));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Refresh_With_Unknown_Token_Is_Rejected()
    {
        await SeedUserAsync();
        var service = CreateService();

        var act = async () => await service.RefreshAsync(new RefreshRequest("not-a-real-token"));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Refresh_Is_Rejected_Once_The_User_Is_Deactivated()
    {
        var user = await SeedUserAsync();
        var service = CreateService();

        var login = await service.LoginAsync(new LoginRequest(user.Email, CorrectPassword));

        user.IsActive = false;
        await _db.SaveChangesAsync();

        var act = async () => await service.RefreshAsync(new RefreshRequest(login.RefreshToken));

        await act.Should().ThrowAsync<InvalidCredentialsException>(
            "deactivating an account must end its sessions, not just block new logins");
    }

    // ---------------------------------------------------------------------------------
    // Me
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task GetCurrentUser_Returns_The_Caller_Profile_Without_The_Password_Hash()
    {
        var user = await SeedUserAsync(UserRole.Teacher);
        _currentUser.UserId = user.Id;

        var service = CreateService();

        var profile = await service.GetCurrentUserAsync();

        profile.Id.Should().Be(user.Id);
        profile.Email.Should().Be(user.Email);
        profile.Role.Should().Be(UserRole.Teacher);

        typeof(UserProfile).GetProperties()
            .Should().NotContain(p => p.Name.Contains("Password"),
                "the profile DTO must not carry credentials");
    }
}
