using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Drivers.Commands.UpdateDriverProfile;

public sealed record UpdateDriverProfileCommand(
    Guid RequestingUserId, Guid DriverProfileId, string DriverLicenseNumber, DateTime DriverLicenseExpiration,
    string? TaxiLicenseNumber) : ICommand<Result>;
