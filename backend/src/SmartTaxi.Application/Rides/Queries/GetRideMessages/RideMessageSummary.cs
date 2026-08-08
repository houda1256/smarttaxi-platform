using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Queries.GetRideMessages;

public sealed record RideMessageSummary(Guid Id, Guid SenderId, RideMessageType MessageType, string Content, bool IsReported, DateTime SentAt)
{
    public static RideMessageSummary FromEntity(RideMessage message) =>
        new(message.Id, message.SenderId, message.MessageType, message.Content, message.IsReported, message.SentAt);
}
