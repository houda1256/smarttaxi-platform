using SmartTaxi.Domain.Fleet.Owners.Entities;

namespace SmartTaxi.Application.Fleet.Owners.Abstractions;

public interface ITaxiOwnerProfileRepository
{
    Task AddAsync(TaxiOwnerProfile profile, CancellationToken cancellationToken);

    Task<TaxiOwnerProfile?> GetByIdAsync(Guid ownerId, CancellationToken cancellationToken);

    Task<TaxiOwnerProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task UpdateAsync(TaxiOwnerProfile profile, CancellationToken cancellationToken);
}
