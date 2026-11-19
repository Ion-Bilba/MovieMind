using System.ComponentModel.DataAnnotations;

namespace MovieMind.Application.Dtos.Preferences;

public sealed class UpdateUserPreferencesRequest
{
    [Required]
    public List<GenrePreferenceInput> GenrePreferences { get; init; } = [];
}

public sealed class GenrePreferenceInput
{
    [Required]
    public Guid GenreId { get; init; }

    [Range(0, 10)]
    public double Score { get; init; }
}