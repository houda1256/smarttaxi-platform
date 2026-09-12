using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.UpdateRoadsidePartnerProfile;

public sealed record UpdateRoadsidePartnerProfileCommand(
    Guid UserId, string BusinessName, string? LegalName, string Address, string City, string? SupportedServiceTypes,
    string? SupportedVehicleCategories, double? Latitude, double? Longitude) : ICommand<Result>;
