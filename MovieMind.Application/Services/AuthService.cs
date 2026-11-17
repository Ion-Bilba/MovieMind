using Microsoft.EntityFrameworkCore;
using MovieMind.Application.DTOs.Auth;
using MovieMind.Application.Exceptions;
using MovieMind.Application.Interfaces;
using MovieMind.Domain.Entities;
using MovieMind.Domain.Enums;

namespace MovieMind.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        IApplicationDbContext context,
        IPasswordHashService passwordHashService,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _passwordHashService = passwordHashService;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var userName = request.UserName.Trim();
        var email = request.Email.Trim();

        var normalizedUserName = userName.ToUpperInvariant();
        var normalizedEmail = email.ToUpperInvariant();

        var alreadyExists = await _context.Users.AnyAsync(
            user =>
                user.UserName.ToUpper() == normalizedUserName ||
                user.Email.ToUpper() == normalizedEmail,
            cancellationToken);

        if (alreadyExists)
        {
            throw new DuplicateUserException();
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = email,
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash =
            _passwordHashService.HashPassword(user, request.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var identifier = request.Identifier.Trim().ToUpperInvariant();

        var user = await _context.Users.SingleOrDefaultAsync(
            candidate =>
                candidate.UserName.ToUpper() == identifier ||
                candidate.Email.ToUpper() == identifier,
            cancellationToken);

        if (user is null ||
            !_passwordHashService.VerifyPassword(user, request.Password))
        {
            return null;
        }

        return CreateAuthResponse(user);
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var token = _jwtTokenGenerator.Generate(user);

        return new AuthResponse(
            user.Id,
            user.UserName,
            user.Email,
            user.Role.ToString(),
            token.Value,
            token.ExpiresAtUtc);
    }
}