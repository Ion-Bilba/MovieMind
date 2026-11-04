using MovieMind.Application.Dtos.Movies;

namespace MovieMind.Application.Interfaces;

public interface IMovieService
{
    Task<List<MovieDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<MovieDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<MovieDto> CreateAsync(
        CreateMovie request,
        CancellationToken cancellationToken = default);

    Task<MovieDto?> UpdateAsync(
        Guid id,
        UpdateMovie request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}