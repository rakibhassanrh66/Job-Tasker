// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Application.Users.Dtos;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AssignmentSystem.Application.Users;

public interface IUserService
{
    Task<PagedResult<UserDto>> ListAsync(UserListQuery query, CancellationToken cancellationToken = default);

    Task<UserDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);

    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}

public class UserService : IUserService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IAppDbContext db,
        IPasswordHasher passwordHasher,
        ICurrentUser currentUser,
        IClock clock,
        ILogger<UserService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    public async Task<PagedResult<UserDto>> ListAsync(
        UserListQuery query, CancellationToken cancellationToken = default)
    {
        var users = _db.Users.AsNoTracking();

        if (query.Role is not null)
        {
            users = users.Where(u => u.Role == query.Role);
        }

        if (query.IsActive is not null)
        {
            users = users.Where(u => u.IsActive == query.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();

            users = users.Where(u =>
                u.FullName.ToLower().Contains(term) || u.Email.Contains(term));
        }

        return await users
            .OrderBy(u => u.Role)
            .ThenBy(u => u.FullName)
            .Select(u => new UserDto(u.Id, u.FullName, u.Email, u.Role, u.IsActive, u.CreatedAt))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<UserDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserDto(u.Id, u.FullName, u.Email, u.Role, u.IsActive, u.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        return user ?? throw new NotFoundException("User", id);
    }

    public async Task<UserDto> CreateAsync(
        CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            throw DuplicateResourceException.Email(email);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = email,
            Role = request.Role,
            IsActive = true,
            CreatedAt = _clock.UtcNow,
            PasswordHash = _passwordHasher.Hash(request.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created {Role} account {Email}.", user.Role, user.Email);

        return new UserDto(user.Id, user.FullName, user.Email, user.Role, user.IsActive, user.CreatedAt);
    }

    public async Task<UserDto> UpdateAsync(
        Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NotFoundException("User", id);

        if (!request.IsActive && id == _currentUser.UserId)
        {
            throw new ResourceInUseException(
                "You cannot deactivate your own account.");
        }

        user.FullName = request.FullName.Trim();
        user.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        return new UserDto(user.Id, user.FullName, user.Email, user.Role, user.IsActive, user.CreatedAt);
    }

    /// <summary>
    /// Deactivates rather than deletes.
    ///
    /// A user is referenced by the assignments they authored and the submissions they made
    /// or graded. Removing the row would either destroy that history or orphan it, so the
    /// account is disabled instead — login is refused and existing sessions stop
    /// refreshing, while the record stays intact.
    /// </summary>
    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NotFoundException("User", id);

        if (id == _currentUser.UserId)
        {
            // An administrator disabling their own account could lock every administrator
            // out of the system with no way back in through the API.
            throw new ResourceInUseException("You cannot deactivate your own account.");
        }

        if (!user.IsActive)
        {
            return;
        }

        user.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deactivated account {Email}.", user.Email);
    }
}
