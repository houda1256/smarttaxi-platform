using SmartTaxi.Application.Common;
using SmartTaxi.Application.RoadsideAssistance.Commands.DisputeRoadsideAssistanceRequest;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.Tests.RoadsideAssistance.Commands;

public class DisputeRoadsideAssistanceRequestCommandHandlerTests
{
    private readonly FakeRoadsideAssistanceRequestRepository _requestRepository = new();
    private readonly DisputeRoadsideAssistanceRequestCommandHandler _handler;

    public DisputeRoadsideAssistanceRequestCommandHandlerTests()
    {
        _handler = new DisputeRoadsideAssistanceRequestCommandHandler(_requestRepository);
    }

    private async Task<RoadsideAssistanceRequest> CreateCompletedRequestAsync()
    {
        var request = RoadsideAssistanceRequest.Create(
            Guid.NewGuid(), RoadsideRequesterRole.TaxiOwner, Guid.NewGuid(), null, RoadsideServiceType.Towing, RoadsideUrgency.High, "Panne", 36.8,
            10.18, null, null, DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);
        await _requestRepository.TryTransitionAsync(
            request.Id, [RoadsideRequestStatus.PartnersAvailable], RoadsideRequestStatus.Completed, null, null, 80m, null, null, false,
            DateTime.UtcNow, CancellationToken.None);
        return request;
    }

    [Fact]
    public async Task Handle_CompletedUnsettledRequest_MovesToDisputed()
    {
        var request = await CreateCompletedRequestAsync();

        var result = await _handler.Handle(new DisputeRoadsideAssistanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(RoadsideRequestStatus.Disputed, reloaded!.Status);
    }

    [Fact]
    public async Task Handle_AlreadySettledRequest_ReturnsConflict()
    {
        var request = await CreateCompletedRequestAsync();
        await _requestRepository.TryMarkSettledAsync(request.Id, DateTime.UtcNow, CancellationToken.None);

        var result = await _handler.Handle(new DisputeRoadsideAssistanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Handle_NotCompletedRequest_ReturnsConflict()
    {
        var request = RoadsideAssistanceRequest.Create(
            Guid.NewGuid(), RoadsideRequesterRole.TaxiOwner, Guid.NewGuid(), null, RoadsideServiceType.Towing, RoadsideUrgency.High, "Panne", 36.8,
            10.18, null, null, DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);

        var result = await _handler.Handle(new DisputeRoadsideAssistanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}
