using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeDriverReservationHoldRepository : IDriverReservationHoldRepository
{
    private readonly Dictionary<Guid, DriverReservationHold> _holdsById = new();

    public Task<bool> TryAddAsync(DriverReservationHold hold, CancellationToken cancellationToken)
    {
        var alreadyActiveForDriver = _holdsById.Values.Any(h =>
            h.DriverId == hold.DriverId && h.Status == DriverReservationHoldStatus.Active);

        if (alreadyActiveForDriver)
        {
            return Task.FromResult(false);
        }

        _holdsById[hold.Id] = hold;
        return Task.FromResult(true);
    }

    public Task<DriverReservationHold?> GetByIdAsync(Guid holdId, CancellationToken cancellationToken) =>
        Task.FromResult(_holdsById.GetValueOrDefault(holdId));

    public Task<DriverReservationHold?> GetActiveForRideAsync(Guid rideId, CancellationToken cancellationToken)
    {
        var hold = _holdsById.Values.FirstOrDefault(h => h.RideId == rideId && h.Status == DriverReservationHoldStatus.Active);
        return Task.FromResult(hold);
    }

    public Task<DriverReservationHold?> GetActiveForDriverAsync(Guid driverId, CancellationToken cancellationToken)
    {
        var hold = _holdsById.Values.FirstOrDefault(h => h.DriverId == driverId && h.Status == DriverReservationHoldStatus.Active);
        return Task.FromResult(hold);
    }

    public Task<bool> TryAcceptAsync(Guid holdId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(holdId, DriverReservationHoldStatus.Accepted);

    public Task<bool> TryReleaseAsync(Guid holdId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(holdId, DriverReservationHoldStatus.Released);

    public Task<bool> TryExpireAsync(Guid holdId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(holdId, DriverReservationHoldStatus.Expired);

    private Task<bool> TryTransition(Guid holdId, DriverReservationHoldStatus to)
    {
        if (!_holdsById.TryGetValue(holdId, out var hold) || hold.Status != DriverReservationHoldStatus.Active)
        {
            return Task.FromResult(false);
        }

        typeof(DriverReservationHold).GetProperty(nameof(DriverReservationHold.Status))!.SetValue(hold, to);
        return Task.FromResult(true);
    }
}
