using MovieMind.Application.Dtos.Actors;
using System;
using System.Collections.Generic;
using System.Text;

namespace MovieMind.Application.Interfaces
{
    public interface IActorService
    {
        Task<List<ActorDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<ActorDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> NameExistsAsync(
            string name,
            Guid? excludeId = null,
            CancellationToken cancellationToken = default);
        Task<ActorDto> CreateAsync(
            CreateActor request,
            CancellationToken cancellationToken = default);
        Task<ActorDto?> UpdateAsync(
            Guid id,
            UpdateActor request,
            CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
