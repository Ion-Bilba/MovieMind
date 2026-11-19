using MovieMind.Application.Dtos.Preferences;

namespace MovieMind.Application.Interfaces;

public interface IUserPreferencesService
{
    Task<UserPreferencesDto?> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserPreferencesDto?> UpdateAsync(
        Guid userId,
        UpdateUserPreferencesRequest request,
        CancellationToken cancellationToken = default);
}