using MediatR;
using NetworkMonitor.Application.Common.Exceptions;
using NetworkMonitor.Application.DTOs;
using NetworkMonitor.Application.Interfaces;
namespace NetworkMonitor.Application.Features.Authentication;

public record LoginCommand(string Username, string Password) : IRequest<AuthResultDto>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResultDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IDateTimeProvider _clock;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IDateTimeProvider clock)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _clock = clock;
    }

    public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);

        // Deliberately the same error message whether the username doesn't
        // exist or the password is wrong — distinguishing the two would let
        // an attacker enumerate valid usernames.
        if (user is null || !user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new AppAuthenticationException("Invalid username or password.");
        }

        var token = _jwtTokenGenerator.GenerateToken(user);

        user.LastLoginAt = _clock.UtcNow;
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return new AuthResultDto
        {
            AccessToken = token.AccessToken,
            ExpiresAt = token.ExpiresAt,
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt
            }
        };
    }
}

/// <summary>Backs GET /api/auth/me — hydrates Angular app state from the JWT-derived identity after a page refresh.</summary>
public record GetCurrentUserQuery : IRequest<UserDto>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserRepository _userRepository;

    public GetCurrentUserQueryHandler(ICurrentUserService currentUserService, IUserRepository userRepository)
    {
        _currentUserService = currentUserService;
        _userRepository = userRepository;
    }

    public async Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
        {
            throw new AppAuthenticationException("No authenticated user for the current request.");
        }

        var user = await _userRepository.GetByIdAsync(_currentUserService.UserId.Value, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), _currentUserService.UserId.Value);

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt
        };
    }
}
