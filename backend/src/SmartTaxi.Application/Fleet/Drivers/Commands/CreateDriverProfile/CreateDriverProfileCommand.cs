using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Drivers.Commands.CreateDriverProfile;

public sealed record CreateDriverProfileCommand(
    Guid UserId, string DriverLicenseNumber, DateTime DriverLicenseExpiration, string? TaxiLicenseNumber,
    bool IndependentDriver) : ICommand<Result<Guid>>;
