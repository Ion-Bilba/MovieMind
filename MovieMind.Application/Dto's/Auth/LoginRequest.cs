using System.ComponentModel.DataAnnotations;

namespace MovieMind.Application.DTOs.Auth;

public sealed class LoginRequest
{
    [Required]
    [StringLength(254)]
    public string Identifier { get; init; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;
}