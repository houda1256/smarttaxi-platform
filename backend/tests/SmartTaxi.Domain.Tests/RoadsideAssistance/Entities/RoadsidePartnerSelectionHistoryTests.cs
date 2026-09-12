using SmartTaxi.Domain.RoadsideAssistance.Entities;

namespace SmartTaxi.Domain.Tests.RoadsideAssistance.Entities;

public class RoadsidePartnerSelectionHistoryTests
{
    [Fact]
    public void Constructor_SetsFieldsAndLeavesResponsePending()
    {
        var requestId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var history = new RoadsidePartnerSelectionHistory(requestId, 1, partnerId, now);

        Assert.NotEqual(Guid.Empty, history.Id);
        Assert.Equal(requestId, history.RoadsideAssistanceRequestId);
        Assert.Equal(1, history.CycleNumber);
        Assert.Equal(partnerId, history.SelectedPartnerUserId);
        Assert.Equal(now, history.SelectedAtUtc);
        Assert.Null(history.Response);
        Assert.Null(history.RespondedAtUtc);
        Assert.Null(history.RejectionReason);
    }
}
