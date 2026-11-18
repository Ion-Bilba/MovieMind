namespace MovieMind.Application.DTOs.Auth;

public sealed record AuthResponse(
    Guid Id,
    string UserName,
    string Email,
    string Role,
    string AccessToken,
    DateTime ExpiresAtUtc);

public sealed record IssuedAccessToken(
    string Value,
    DateTime ExpiresAtUtc);