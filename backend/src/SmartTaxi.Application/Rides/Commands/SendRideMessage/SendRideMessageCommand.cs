using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.SendRideMessage;

/// <summary>The Ride-linked conversation is auto-created on first message — no separate "start conversation" step is required.</summary>
public sealed record SendRideMessageCommand(Guid RequestingUserId, Guid RideId, RideMessageType MessageType, string Content)
    : ICommand<Result<Guid>>;
