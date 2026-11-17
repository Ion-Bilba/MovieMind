using MovieMind.Application.DTOs.Auth;
using MovieMind.Domain.Entities;

namespace MovieMind.Application.Interfaces;

public interface IJwtTokenGenerator
{
    IssuedAccessToken Generate(User user);
}