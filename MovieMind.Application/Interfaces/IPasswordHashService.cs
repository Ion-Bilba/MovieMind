using MovieMind.Domain.Entities;

namespace MovieMind.Application.Interfaces;

public interface IPasswordHashService
{
    string HashPassword(User user, string password);

    bool VerifyPassword(User user, string password);
}