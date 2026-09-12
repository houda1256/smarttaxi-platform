using SmartTaxi.Application.Common;
using SmartTaxi.Application.RoadsideAssistance.Commands.SettleRoadsideAssistanceRequest;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.Tests.RoadsideAssistance.Commands;

/// <summary>Mirrors SettleMaintenanceRequestCommandHandlerTests exactly — the same hardened Module 8/9 settlement-recovery design, applied to Roadside Assistance from day one.</summary>
public class SettleRoadsideAssistanceRequestCommandHandlerTests
{
    private readonly FakeRoadsideAssistanceRequestRepository _requestRepository = new();
    private readonly FakeRoadsideAssistanceBillingService _billingService = new();
    private readonly SettleRoadsideAssistanceRequestCommandHandler _handler;

    public SettleRoadsideAssistanceRequestCommandHandlerTests()
    {
        _handler = new SettleRoadsideAssistanceRequestCommandHandler(_requestRepository, _billingService);
    }

    private async Task<RoadsideAssistanceRequest> CreateCompletedRequestAsync(decimal finalCost)
    {
        var requesterUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var request = RoadsideAssistanceRequest.Create(
            requesterUserId, RoadsideRequesterRole.TaxiOwner, Guid.NewGuid(), null, RoadsideServiceType.BatteryJumpStart, RoadsideUrgency.Low,
            "Batterie", 36.8, 10.18, null, null, DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);
        await _requestRepository.TrySelectPartnerAsync(request.Id, requesterUserId, partnerUserId, DateTime.UtcNow, CancellationToken.None);
        await _requestRepository.TryRespondAsync(
            request.Id, partnerUserId, RoadsidePartnerResponse.Accepted, null, null, DateTime.UtcNow, CancellationToken.None);
        await _requestRepository.TryTransitionAsync(
            request.Id, [RoadsideRequestStatus.Accepted], RoadsideRequestStatus.PartnerOnTheWay, null, partnerUserId, null, null, null, false,
            DateTime.UtcNow, CancellationToken.None);
        await _requestRepository.TryTransitionAsync(
            request.Id, [RoadsideRequestStatus.PartnerOnTheWay], RoadsideRequestStatus.PartnerArrived, null, partnerUserId, null, null, null, false,
            DateTime.UtcNow, CancellationToken.None);
        await _requestRepository.TryTransitionAsync(
            request.Id, [RoadsideRequestStatus.PartnerArrived], RoadsideRequestStatus.InProgress, null, partnerUserId, null, null, null, false,
            DateTime.UtcNow, CancellationToken.None);
        await _requestRepository.TryTransitionAsync(
            request.Id, [RoadsideRequestStatus.InProgress], RoadsideRequestStatus.Completed, null, partnerUserId, finalCost, null, null, false,
            DateTime.UtcNow, CancellationToken.None);
        return request;
    }

    [Fact]
    public async Task Handle_EligibleRequest_SettlesSuccessfully()
    {
        var request = await CreateCompletedRequestAsync(80m);

        var result = await _handler.Handle(new SettleRoadsideAssistanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(_billingService.SettledCalls);
        var reloaded = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.NotNull(reloaded!.SettledAtUtc);
    }

    [Fact]
    public async Task Handle_AlreadySettled_ReturnsConflictWithoutPostingAgain()
    {
        var request = await CreateCompletedRequestAsync(80m);
        var first = await _handler.Handle(new SettleRoadsideAssistanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None);
        Assert.True(first.IsSuccess);

        var second = await _handler.Handle(new SettleRoadsideAssistanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(second.IsSuccess);
        Assert.Equal(ErrorType.Conflict, second.ErrorType);
        Assert.Single(_billingService.SettledCalls);
    }

    [Fact]
    public async Task Handle_NotCompletedStatus_ReturnsConflict()
    {
        var request = RoadsideAssistanceRequest.Create(
            Guid.NewGuid(), RoadsideRequesterRole.TaxiOwner, Guid.NewGuid(), null, RoadsideServiceType.Towing, RoadsideUrgency.High, "Panne", 36.8,
            10.18, null, null, DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);

        var result = await _handler.Handle(new SettleRoadsideAssistanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Empty(_billingService.SettledCalls);
    }

    /// <summary>The core recovery scenario: the ledger entry was already posted (as if PostBatchAsync had committed in a prior, crashed attempt) but SettledAtUtc is still null. The retry must converge without ever posting a second ledger entry.</summary>
    [Fact]
    public async Task Handle_OrphanedLedgerEntryFromPriorCrash_ConvergesWithoutDoublePosting()
    {
        var request = await CreateCompletedRequestAsync(80m);
        _billingService.RequestIdsWithOrphanedLedgerEntry.Add(request.Id);

        var result = await _handler.Handle(new SettleRoadsideAssistanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_billingService.SettledCalls);
        var reloaded = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.NotNull(reloaded!.SettledAtUtc);
    }

    [Fact]
    public async Task Handle_RequestNotFound_ReturnsNotFound()
    {
        var result = await _handler.Handle(new SettleRoadsideAssistanceRequestCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
