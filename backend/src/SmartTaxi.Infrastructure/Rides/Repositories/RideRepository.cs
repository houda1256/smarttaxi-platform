using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Rides.Repositories;

/// <summary>
/// Mirrors RideStatusTransitions.TerminalStatuses (kept private to the Domain
/// project) so "active ride" queries stay translatable to SQL — the same
/// duplication pattern as DriverVehicleAssignmentRepository.NonTerminalStatuses.
/// </summary>
internal sealed class RideRepository : IRideRepository
{
    private static readonly RideStatus[] TerminalStatuses =
    [
        RideStatus.Completed, RideStatus.CancelledByCustomer, RideStatus.CancelledByDriver,
        RideStatus.CancelledByAdmin, RideStatus.Expired, RideStatus.CustomerNoShow
    ];

    private readonly ApplicationDbContext _context;

    public RideRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Ride ride, CancellationToken cancellationToken)
    {
        await _context.Rides.AddAsync(ride, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<Ride?> GetByIdAsync(Guid rideId, CancellationToken cancellationToken) =>
        _context.Rides.FirstOrDefaultAsync(ride => ride.Id == rideId, cancellationToken);

    public async Task<IReadOnlyCollection<Ride>> GetForCustomerAsync(Guid customerId, CancellationToken cancellationToken) =>
        await _context.Rides
            .Where(ride => ride.CustomerId == customerId)
            .OrderByDescending(ride => ride.RequestedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Ride>> GetForDriverAsync(Guid driverId, CancellationToken cancellationToken) =>
        await _context.Rides
            .Where(ride => ride.SelectedDriverId == driverId)
            .OrderByDescending(ride => ride.RequestedAt)
            .ToListAsync(cancellationToken);

    public Task<Ride?> GetActiveForDriverAsync(Guid driverId, CancellationToken cancellationToken) =>
        _context.Rides.FirstOrDefaultAsync(
            ride => ride.SelectedDriverId == driverId && !TerminalStatuses.Contains(ride.Status), cancellationToken);

    public async Task<IReadOnlyCollection<Ride>> GetAllAsync(RideStatus? status, CancellationToken cancellationToken)
    {
        var query = _context.Rides.AsQueryable();

        if (status is not null)
        {
            query = query.Where(ride => ride.Status == status);
        }

        return await query.OrderByDescending(ride => ride.RequestedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Ride>> GetActiveAsync(CancellationToken cancellationToken) =>
        await _context.Rides
            .Where(ride => !TerminalStatuses.Contains(ride.Status))
            .OrderByDescending(ride => ride.RequestedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Ride>> GetPendingSharedRidesAsync(CancellationToken cancellationToken) =>
        await _context.Rides
            .Where(ride => ride.RideType == RideType.Shared
                && (ride.Status == RideStatus.Searching || ride.Status == RideStatus.DriversAvailable))
            .ToListAsync(cancellationToken);

    public Task UpdateAsync(Ride ride, CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);

    public async Task<bool> TryTransitionAsync(
        Guid rideId, RideStatus from, RideStatus to, Guid? changedBy, string? reason, DateTime utcNow,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var rows = await _context.Rides
            .Where(ride => ride.Id == rideId && ride.Status == from)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(ride => ride.Status, to)
                .SetProperty(ride => ride.UpdatedAt, utcNow), cancellationToken);

        if (rows != 1)
        {
            return false;
        }

        await _context.RideStatusHistories.AddAsync(new RideStatusHistory(rideId, from, to, changedBy, reason, utcNow), cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TrySelectDriverAsync(
        Guid rideId, Guid driverId, Guid vehicleId, DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var rows = await _context.Rides
            .Where(ride => ride.Id == rideId && ride.Status == RideStatus.DriversAvailable)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(ride => ride.SelectedDriverId, driverId)
                .SetProperty(ride => ride.VehicleId, vehicleId)
                .SetProperty(ride => ride.Status, RideStatus.PendingDriverResponse)
                .SetProperty(ride => ride.UpdatedAt, utcNow), cancellationToken);

        if (rows != 1)
        {
            return false;
        }

        await _context.RideStatusHistories.AddRangeAsync(
        [
            new RideStatusHistory(rideId, RideStatus.DriversAvailable, RideStatus.DriverSelected, null, null, utcNow),
            new RideStatusHistory(rideId, RideStatus.DriverSelected, RideStatus.PendingDriverResponse, null, null, utcNow)
        ], cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryApplyNegotiatedFareAsync(Guid rideId, decimal amount, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.Rides
            .Where(ride => ride.Id == rideId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(ride => ride.NegotiatedFinalFare, amount)
                .SetProperty(ride => ride.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryConfirmSharedRideDriverAsync(
        Guid rideId, Guid driverId, Guid vehicleId, DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var currentStatus = await _context.Rides
            .Where(ride => ride.Id == rideId)
            .Select(ride => (RideStatus?)ride.Status)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentStatus is not (RideStatus.Searching or RideStatus.DriversAvailable))
        {
            return false;
        }

        var rows = await _context.Rides
            .Where(ride => ride.Id == rideId && ride.Status == currentStatus)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(ride => ride.SelectedDriverId, driverId)
                .SetProperty(ride => ride.VehicleId, vehicleId)
                .SetProperty(ride => ride.Status, RideStatus.DriverAccepted)
                .SetProperty(ride => ride.UpdatedAt, utcNow), cancellationToken);

        if (rows != 1)
        {
            return false;
        }

        await _context.RideStatusHistories.AddAsync(
            new RideStatusHistory(rideId, currentStatus, RideStatus.DriverAccepted, driverId, "Confirmé via trajet partagé", utcNow),
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryUpdateLastKnownLocationAsync(
        Guid rideId, double latitude, double longitude, DateTime recordedAt, CancellationToken cancellationToken)
    {
        var rows = await _context.Rides
            .Where(ride => ride.Id == rideId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(ride => ride.LastKnownLatitude, latitude)
                .SetProperty(ride => ride.LastKnownLongitude, longitude)
                .SetProperty(ride => ride.LastLocationRecordedAt, recordedAt), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryMarkDriverArrivedAsync(Guid rideId, DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var rows = await _context.Rides
            .Where(ride => ride.Id == rideId && ride.Status == RideStatus.DriverEnRoute)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(ride => ride.Status, RideStatus.DriverArrived)
                .SetProperty(ride => ride.DriverArrivedAt, utcNow)
                .SetProperty(ride => ride.UpdatedAt, utcNow), cancellationToken);

        if (rows != 1)
        {
            return false;
        }

        await _context.RideStatusHistories.AddAsync(
            new RideStatusHistory(rideId, RideStatus.DriverEnRoute, RideStatus.DriverArrived, null, null, utcNow), cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryCompleteAsync(
        Guid rideId, decimal actualDistanceKm, int actualDurationMinutes, decimal finalFare, DateTime utcNow,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var rows = await _context.Rides
            .Where(ride => ride.Id == rideId && ride.Status == RideStatus.InProgress)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(ride => ride.ActualDistanceKm, actualDistanceKm)
                .SetProperty(ride => ride.ActualDurationMinutes, actualDurationMinutes)
                .SetProperty(ride => ride.FinalFare, finalFare)
                .SetProperty(ride => ride.Status, RideStatus.AwaitingPayment)
                .SetProperty(ride => ride.UpdatedAt, utcNow), cancellationToken);

        if (rows != 1)
        {
            return false;
        }

        await _context.RideStatusHistories.AddAsync(
            new RideStatusHistory(rideId, RideStatus.InProgress, RideStatus.AwaitingPayment, null, null, utcNow), cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
