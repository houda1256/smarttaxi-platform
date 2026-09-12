using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Vehicles.Commands.SubmitVehicleForVerification;

public sealed record SubmitVehicleForVerificationCommand(Guid RequestingUserId, Guid VehicleId) : ICommand<Result>;
