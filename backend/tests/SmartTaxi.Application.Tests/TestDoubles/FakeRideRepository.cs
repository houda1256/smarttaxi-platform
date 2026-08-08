using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeRideRepository : IRideRepository
{
    private readonly Dictionary<Guid, Ride> _ridesById = new();
    private readonly List<RideStatusHistory> _history = [];

    public Task AddAsync(Ride ride, CancellationToken cancellationToken)
    {
        _ridesById[ride.Id] = ride;
        return Task.CompletedTask;
    }

    public Task<Ride?> GetByIdAsync(Guid rideId, CancellationToken cancellationToken) =>
        Task.FromResult(_ridesById.GetValueOrDefault(rideId));

    public Task<IReadOnlyCollection<Ride>> GetForCustomerAsync(Guid customerId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Ride> rides = _ridesById.Values.Where(r => r.CustomerId == customerId).ToList();
        return Task.FromResult(rides);
    }

    public Task<IReadOnlyCollection<Ride>> GetForDriverAsync(Guid driverId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Ride> rides = _ridesById.Values.Where(r => r.SelectedDriverId == driverId).ToList();
        return Task.FromResult(rides);
    }

    public Task<Ride?> GetActiveForDriverAsync(Guid driverId, CancellationToken cancellationToken)
    {
        var ride = _ridesById.Values.FirstOrDefault(r =>
            r.SelectedDriverId == driverId && !RideStatusTransitions.IsTerminal(r.Status));
        return Task.FromResult(ride);
    }

    public Task<IReadOnlyCollection<Ride>> GetAllAsync(RideStatus? status, CancellationToken cancellationToken)
    {
        var query = _ridesById.Values.AsEnumerable();

        if (status is not null)
        {
            query = query.Where(r => r.Status == status);
        }

        IReadOnlyCollection<Ride> rides = query.ToList();
        return Task.FromResult(rides);
    }

    public Task<IReadOnlyCollection<Ride>> GetActiveAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Ride> rides = _ridesById.Values.Where(r => !RideStatusTransitions.IsTerminal(r.Status)).ToList();
        return Task.FromResult(rides);
    }

    public Task<IReadOnlyCollection<Ride>> GetPendingSharedRidesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Ride> rides = _ridesById.Values
            .Where(r => r.RideType == RideType.Shared && r.Status is RideStatus.Searching or RideStatus.DriversAvailable)
            .ToList();
        return Task.FromResult(rides);
    }

    public Task UpdateAsync(Ride ride, CancellationToken cancellationToken)
    {
        _ridesById[ride.Id] = ride;
        return Task.CompletedTask;
    }

    public Task<bool> TryTransitionAsync(
        Guid rideId, RideStatus from, RideStatus to, Guid? changedBy, string? reason, DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (!_ridesById.TryGetValue(rideId, out var ride) || ride.Status != from)
        {
            return Task.FromResult(false);
        }

        SetProperty(ride, nameof(Ride.Status), to);
        SetProperty(ride, nameof(Ride.UpdatedAt), utcNow);
        _history.Add(new RideStatusHistory(rideId, from, to, changedBy, reason, utcNow));
        return Task.FromResult(true);
    }

    public Task<bool> TrySelectDriverAsync(Guid rideId, Guid driverId, Guid vehicleId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_ridesById.TryGetValue(rideId, out var ride) || ride.Status != RideStatus.DriversAvailable)
        {
            return Task.FromResult(false);
        }

        SetProperty(ride, nameof(Ride.SelectedDriverId), driverId);
        SetProperty(ride, nameof(Ride.VehicleId), vehicleId);
        SetProperty(ride, nameof(Ride.Status), RideStatus.PendingDriverResponse);
        SetProperty(ride, nameof(Ride.UpdatedAt), utcNow);
        _history.Add(new RideStatusHistory(rideId, RideStatus.DriversAvailable, RideStatus.DriverSelected, null, null, utcNow));
        _history.Add(new RideStatusHistory(rideId, RideStatus.DriverSelected, RideStatus.PendingDriverResponse, null, null, utcNow));
        return Task.FromResult(true);
    }

    public Task<bool> TryApplyNegotiatedFareAsync(Guid rideId, decimal amount, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_ridesById.TryGetValue(rideId, out var ride))
        {
            return Task.FromResult(false);
        }

        SetProperty(ride, nameof(Ride.NegotiatedFinalFare), amount);
        SetProperty(ride, nameof(Ride.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryConfirmSharedRideDriverAsync(
        Guid rideId, Guid driverId, Guid vehicleId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_ridesById.TryGetValue(rideId, out var ride)
            || ride.Status is not (RideStatus.Searching or RideStatus.DriversAvailable))
        {
            return Task.FromResult(false);
        }

        var previousStatus = ride.Status;
        SetProperty(ride, nameof(Ride.SelectedDriverId), driverId);
        SetProperty(ride, nameof(Ride.VehicleId), vehicleId);
        SetProperty(ride, nameof(Ride.Status), RideStatus.DriverAccepted);
        SetProperty(ride, nameof(Ride.UpdatedAt), utcNow);
        _history.Add(new RideStatusHistory(rideId, previousStatus, RideStatus.DriverAccepted, driverId, "Confirmé via trajet partagé", utcNow));
        return Task.FromResult(true);
    }

    public Task<bool> TryUpdateLastKnownLocationAsync(
        Guid rideId, double latitude, double longitude, DateTime recordedAt, CancellationToken cancellationToken)
    {
        if (!_ridesById.TryGetValue(rideId, out var ride))
        {
            return Task.FromResult(false);
        }

        SetProperty(ride, nameof(Ride.LastKnownLatitude), latitude);
        SetProperty(ride, nameof(Ride.LastKnownLongitude), longitude);
        SetProperty(ride, nameof(Ride.LastLocationRecordedAt), recordedAt);
        return Task.FromResult(true);
    }

    public Task<bool> TryMarkDriverArrivedAsync(Guid rideId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_ridesById.TryGetValue(rideId, out var ride) || ride.Status != RideStatus.DriverEnRoute)
        {
            return Task.FromResult(false);
        }

        SetProperty(ride, nameof(Ride.Status), RideStatus.DriverArrived);
        SetProperty(ride, nameof(Ride.DriverArrivedAt), utcNow);
        SetProperty(ride, nameof(Ride.UpdatedAt), utcNow);
        _history.Add(new RideStatusHistory(rideId, RideStatus.DriverEnRoute, RideStatus.DriverArrived, null, null, utcNow));
        return Task.FromResult(true);
    }

    public Task<bool> TryCompleteAsync(
        Guid rideId, decimal actualDistanceKm, int actualDurationMinutes, decimal finalFare, DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (!_ridesById.TryGetValue(rideId, out var ride) || ride.Status != RideStatus.InProgress)
        {
            return Task.FromResult(false);
        }

        SetProperty(ride, nameof(Ride.ActualDistanceKm), actualDistanceKm);
        SetProperty(ride, nameof(Ride.ActualDurationMinutes), actualDurationMinutes);
        SetProperty(ride, nameof(Ride.FinalFare), finalFare);
        SetProperty(ride, nameof(Ride.Status), RideStatus.AwaitingPayment);
        SetProperty(ride, nameof(Ride.UpdatedAt), utcNow);
        _history.Add(new RideStatusHistory(rideId, RideStatus.InProgress, RideStatus.AwaitingPayment, null, null, utcNow));
        return Task.FromResult(true);
    }

    private static void SetProperty(Ride ride, string propertyName, object? value) =>
        typeof(Ride).GetProperty(propertyName)!.SetValue(ride, value);
}
