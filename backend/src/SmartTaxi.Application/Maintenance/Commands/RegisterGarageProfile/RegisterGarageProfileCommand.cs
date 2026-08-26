using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Maintenance.Commands.RegisterGarageProfile;

public sealed record RegisterGarageProfileCommand(
    Guid UserId, string BusinessName, string? LegalName, string Address, string City, string? SupportedVehicleCategories,
    string? AvailableServices) : ICommand<Result<Guid>>;
