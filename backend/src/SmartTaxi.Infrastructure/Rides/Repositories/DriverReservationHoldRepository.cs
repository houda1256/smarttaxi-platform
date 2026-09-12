using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Rides.Repositories;

internal sealed class DriverReservationHoldRepository : IDriverReservationHoldRepository
{
    private readonly ApplicationDbContext _context;

    public DriverReservationHoldRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryAddAsync(DriverReservationHold hold, CancellationToken cancellationToken)
    {
        await _context.DriverReservationHolds.AddAsync(hold, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // The partial unique index on DriverId WHERE Status = 'Active' rejected a second concurrent hold.
            _context.Entry(hold).State = EntityState.Detached;
            return false;
        }
    }

    public Task<DriverReservationHold?> GetByIdAsync(Guid holdId, CancellationToken cancellationToken) =>
        _context.DriverReservationHolds.FirstOrDefaultAsync(hold => hold.Id == holdId, cancellationToken);

    public Task<DriverReservationHold?> GetActiveForRideAsync(Guid rideId, CancellationToken cancellationToken) =>
        _context.DriverReservationHolds.FirstOrDefaultAsync(
            hold => hold.RideId == rideId && hold.Status == DriverReservationHoldStatus.Active, cancellationToken);

    public Task<DriverReservationHold?> GetActiveForDriverAsync(Guid driverId, CancellationToken cancellationToken) =>
        _context.DriverReservationHolds.FirstOrDefaultAsync(
            hold => hold.DriverId == driverId && hold.Status == DriverReservationHoldStatus.Active, cancellationToken);

    public Task<bool> TryAcceptAsync(Guid holdId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionAsync(holdId, DriverReservationHoldStatus.Accepted, cancellationToken);

    public Task<bool> TryReleaseAsync(Guid holdId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionAsync(holdId, DriverReservationHoldStatus.Released, cancellationToken);

    public Task<bool> TryExpireAsync(Guid holdId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionAsync(holdId, DriverReservationHoldStatus.Expired, cancellationToken);

    private async Task<bool> TryTransitionAsync(Guid holdId, DriverReservationHoldStatus to, CancellationToken cancellationToken)
    {
        var rows = await _context.DriverReservationHolds
            .Where(hold => hold.Id == holdId && hold.Status == DriverReservationHoldStatus.Active)
            .ExecuteUpdateAsync(setters => setters.SetProperty(hold => hold.Status, to), cancellationToken);

        return rows == 1;
    }
}
