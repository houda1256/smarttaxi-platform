using SmartTaxi.Domain.Identity.DataRequests.Entities;
using SmartTaxi.Domain.Identity.DataRequests.Enums;

namespace SmartTaxi.Domain.Tests.Identity.DataRequests.Entities;

public class PersonalDataRequestTests
{
    [Fact]
    public void Constructor_SetsInitialStateAsPending()
    {
        var userId = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;

        var request = new PersonalDataRequest(userId, PersonalDataRequestType.Export, utcNow);

        Assert.Equal(userId, request.UserId);
        Assert.Equal(PersonalDataRequestType.Export, request.RequestType);
        Assert.Equal(PersonalDataRequestStatus.Pending, request.Status);
        Assert.Equal(utcNow, request.RequestedAt);
        Assert.Null(request.ProcessedBy);
        Assert.Null(request.ResultReference);
    }
}
