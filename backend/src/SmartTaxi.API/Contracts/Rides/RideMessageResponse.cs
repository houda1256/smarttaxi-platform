using SmartTaxi.Application.Rides.Queries.GetRideMessages;

namespace SmartTaxi.API.Contracts.Rides;

public sealed record RideMessageResponse(Guid Id, Guid SenderId, string MessageType, string Content, bool IsReported, DateTime SentAt)
{
    public static RideMessageResponse FromSummary(RideMessageSummary summary) =>
        new(summary.Id, summary.SenderId, summary.MessageType.ToString(), summary.Content, summary.IsReported, summary.SentAt);
}
