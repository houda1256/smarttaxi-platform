using SmartTaxi.Application.Common;
using SmartTaxi.Application.Maintenance.Queries.GetMaintenanceRequestDetails;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Maintenance.Entities;

namespace SmartTaxi.Application.Tests.Maintenance.Queries;

public class GetMaintenanceRequestDetailsQueryHandlerTests
{
    private readonly FakeMaintenanceRequestRepository _requestRepository = new();
    private readonly GetMaintenanceRequestDetailsQueryHandler _handler;

    public GetMaintenanceRequestDetailsQueryHandlerTests()
    {
        _handler = new GetMaintenanceRequestDetailsQueryHandler(_requestRepository);
    }

    private async Task<MaintenanceRequest> CreateRequestAsync(Guid ownerId, Guid garageUserId)
    {
        var request = MaintenanceRequest.Create(Guid.NewGuid(), ownerId, garageUserId, "Bruit suspect", DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);
        return request;
    }

    [Fact]
    public async Task Handle_AsOwner_Succeeds()
    {
        var ownerId = Guid.NewGuid();
        var request = await CreateRequestAsync(ownerId, Guid.NewGuid());

        var result = await _handler.Handle(new GetMaintenanceRequestDetailsQuery(request.Id, ownerId), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_AsAssignedGarage_Succeeds()
    {
        var garageUserId = Guid.NewGuid();
        var request = await CreateRequestAsync(Guid.NewGuid(), garageUserId);

        var result = await _handler.Handle(new GetMaintenanceRequestDetailsQuery(request.Id, garageUserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_AsUnrelatedUser_ReturnsForbidden()
    {
        var request = await CreateRequestAsync(Guid.NewGuid(), Guid.NewGuid());

        var result = await _handler.Handle(new GetMaintenanceRequestDetailsQuery(request.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }
}
