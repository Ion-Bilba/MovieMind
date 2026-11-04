using MovieMind.Application.Dtos.Genres;

namespace MovieMind.Application.Interfaces;

public interface IGenreService
{
    Task<List<GenreDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<GenreDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
   
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task<GenreDto> CreateAsync(CreateGenreRequest request, CancellationToken cancellationToken = default);
  
    Task<GenreDto?> UpdateAsync(Guid id, UpdateGenreRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}