using Microsoft.AspNetCore.Identity;
using MovieMind.Application.Interfaces;
using MovieMind.Domain.Entities;

namespace MovieMind.Infrastructure.Security;

public sealed class IdentityPasswordHashService : IPasswordHashService
{
    private readonly PasswordHasher<User> _hasher = new();

    public string HashPassword(User user, string password)
    {
        return _hasher.HashPassword(user, password);
    }

    public bool VerifyPassword(User user, string password)
    {
        var result = _hasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password);

        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}