using MediatR;
using NetworkMonitor.Application.Common.Exceptions;
using NetworkMonitor.Application.DTOs;
using NetworkMonitor.Application.Interfaces;
using NetworkMonitor.Domain.Entities;

namespace NetworkMonitor.Application.Features.Users;

/// <summary>
/// Creates a login account. Only reachable by Admins (enforced via
/// [Authorize(Roles = "Admin")] on the API controller — Application doesn't
/// duplicate role checks here, it trusts the API's authorization pipeline).
/// </summary>
public record CreateUserCommand(
    string Username,
    string Email,
    string Password,
    string Role) : IRequest<UserDto>;

public record UpdateUserRoleCommand(Guid Id, string Role) : IRequest<UserDto>;

public record SetUserActiveCommand(Guid Id, bool IsActive) : IRequest;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _clock;

    public CreateUserCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IDateTimeProvider clock)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _clock = clock;
    }

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepository.ExistsWithUsernameOrEmailAsync(request.Username, request.Email, cancellationToken))
        {
            throw new ConflictException($"A user with username '{request.Username}' or that email already exists.");
        }

        var now = _clock.UtcNow;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = request.Role,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return user.ToDto();
    }
}

public class UpdateUserRoleCommandHandler : IRequestHandler<UpdateUserRoleCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IDateTimeProvider _clock;

    public UpdateUserRoleCommandHandler(IUserRepository userRepository, IDateTimeProvider clock)
    {
        _userRepository = userRepository;
        _clock = clock;
    }

    public async Task<UserDto> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.Id);

        user.Role = request.Role;
        user.UpdatedAt = _clock.UtcNow;

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return user.ToDto();
    }
}

public class SetUserActiveCommandHandler : IRequestHandler<SetUserActiveCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IDateTimeProvider _clock;

    public SetUserActiveCommandHandler(IUserRepository userRepository, IDateTimeProvider clock)
    {
        _userRepository = userRepository;
        _clock = clock;
    }

    public async Task Handle(SetUserActiveCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.Id);

        // Deactivating rather than deleting preserves the audit trail of
        // who did what (event log entries don't reference users today, but
        // this keeps the door open without a destructive delete).
        user.IsActive = request.IsActive;
        user.UpdatedAt = _clock.UtcNow;

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);
    }
}

internal static class UserMappingExtensions
{
    public static UserDto ToDto(this User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        Role = user.Role,
        IsActive = user.IsActive,
        LastLoginAt = user.LastLoginAt
    };
}
