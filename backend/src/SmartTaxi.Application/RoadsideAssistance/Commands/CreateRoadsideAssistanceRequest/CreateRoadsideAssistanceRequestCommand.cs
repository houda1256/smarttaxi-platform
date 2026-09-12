using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.CreateRoadsideAssistanceRequest;

public sealed record CreateRoadsideAssistanceRequestCommand(
    Guid RequesterUserId, RoadsideRequesterRole RequesterRole, Guid VehicleId, Guid? RideId, RoadsideServiceType ServiceType,
    RoadsideUrgency Urgency, string Description, double Latitude, double Longitude, string? Address, string? City) : ICommand<Result<Guid>>;
