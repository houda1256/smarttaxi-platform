using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Maintenance.Commands.UpdateGarageProfile;

public sealed record UpdateGarageProfileCommand(
    Guid UserId, string BusinessName, string? LegalName, string Address, string City, string? SupportedVehicleCategories,
    string? AvailableServices) : ICommand<Result>;
