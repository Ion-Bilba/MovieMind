using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MovieMind.Application.Interfaces;
using MovieMind.Domain.Entities;
using MovieMind.Domain.Enums;

namespace MovieMind.Infrastructure.Security;

public sealed class InitialAdminSeeder
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IConfiguration _configuration;

    public InitialAdminSeeder(
        IApplicationDbContext context,
        IPasswordHashService passwordHashService,
        IConfiguration configuration)
    {
        _context = context;
        _passwordHashService = passwordHashService;
        _configuration = configuration;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var userName = _configuration["InitialAdmin:UserName"]?.Trim();
        var email = _configuration["InitialAdmin:Email"]?.Trim();
        var password = _configuration["InitialAdmin:Password"];

        if (string.IsNullOrWhiteSpace(userName) &&
            string.IsNullOrWhiteSpace(email) &&
            string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(userName) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new InvalidOperationException(
                "InitialAdmin needs a username, email, and password of at least 8 characters.");
        }

        if (await _context.Users.AnyAsync(user => user.Role == UserRole.Admin, cancellationToken))
        {
            return;
        }

        var normalizedUserName = userName.ToUpperInvariant();
        var normalizedEmail = email.ToUpperInvariant();
        var conflict = await _context.Users.AnyAsync(
            user => user.UserName.ToUpper() == normalizedUserName ||
                    user.Email.ToUpper() == normalizedEmail,
            cancellationToken);

        if (conflict)
        {
            throw new InvalidOperationException(
                "InitialAdmin username or email already belongs to another account.");
        }

        var admin = new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = email,
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        };
        admin.PasswordHash = _passwordHashService.HashPassword(admin, password);

        _context.Users.Add(admin);
        await _context.SaveChangesAsync(cancellationToken);
    }
}