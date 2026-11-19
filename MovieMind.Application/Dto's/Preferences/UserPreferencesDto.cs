namespace MovieMind.Application.Dtos.Preferences;

public sealed class UserPreferencesDto
{
    public Guid UserId { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public IReadOnlyList<GenrePreferenceDto> GenrePreferences { get; init; } = [];
    public IReadOnlyList<PreferenceVectorEntryDto> Vector { get; init; } = [];
}

public sealed class GenrePreferenceDto
{
    public Guid GenreId { get; init; }
    public string GenreName { get; init; } = string.Empty;
    public double Score { get; init; }
}

public sealed class PreferenceVectorEntryDto
{
    public Guid GenreId { get; init; }
    public string GenreName { get; init; } = string.Empty;
    public double Value { get; init; } // Normalized affinity in the range [-1, 1].
    public string Signal { get; init; } = string.Empty;
}