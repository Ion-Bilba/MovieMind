namespace MovieMind.Domain.Entities;

public sealed class UserPreferenceProfile
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<UserGenrePreference> GenrePreferences { get; set; } = new List<UserGenrePreference>();
}