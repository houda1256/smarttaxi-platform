using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.RegisterRoadsidePartnerProfile;

public sealed record RegisterRoadsidePartnerProfileCommand(
    Guid UserId, string BusinessName, string? LegalName, string Address, string City, string? SupportedServiceTypes,
    string? SupportedVehicleCategories, double? Latitude, double? Longitude) : ICommand<Result<Guid>>;
