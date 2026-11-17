using MovieMind.Application.DTOs.Auth;

namespace MovieMind.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}