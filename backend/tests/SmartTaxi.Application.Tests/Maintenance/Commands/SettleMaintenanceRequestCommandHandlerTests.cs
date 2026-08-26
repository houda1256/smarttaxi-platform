using SmartTaxi.Application.Common;
using SmartTaxi.Application.Maintenance.Commands.SettleMaintenanceRequest;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.Maintenance.Enums;

namespace SmartTaxi.Application.Tests.Maintenance.Commands;

/// <summary>Mirrors SettleCampaignBudgetCommandHandlerTests exactly — the same hardened Module 8 settlement-recovery design, applied to Maintenance from day one (unlike Advertising's first pass, which needed a follow-up fix).</summary>
public class SettleMaintenanceRequestCommandHandlerTests
{
    private readonly FakeMaintenanceRequestRepository _requestRepository = new();
    private readonly FakeMaintenanceBillingService _billingService = new();
    private readonly SettleMaintenanceRequestCommandHandler _handler;

    public SettleMaintenanceRequestCommandHandlerTests()
    {
        _handler = new SettleMaintenanceRequestCommandHandler(_requestRepository, _billingService);
    }

    private async Task<MaintenanceRequest> CreateCompletedRequestAsync(decimal finalCost)
    {
        var request = MaintenanceRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Bruit suspect", DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);
        await _requestRepository.TryTransitionAsync(
            request.Id, [MaintenanceRequestStatus.PendingGarageResponse], MaintenanceRequestStatus.Completed, null, null, null, finalCost, null,
            null, DateTime.UtcNow, CancellationToken.None);
        return request;
    }

    [Fact]
    public async Task Handle_EligibleRequest_SettlesSuccessfully()
    {
        var request = await CreateCompletedRequestAsync(100m);

        var result = await _handler.Handle(new SettleMaintenanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(_billingService.SettledCalls);
        var reloaded = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.NotNull(reloaded!.SettledAtUtc);
    }

    [Fact]
    public async Task Handle_AlreadySettled_ReturnsConflictWithoutPostingAgain()
    {
        var request = await CreateCompletedRequestAsync(100m);
        var first = await _handler.Handle(new SettleMaintenanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None);
        Assert.True(first.IsSuccess);

        var second = await _handler.Handle(new SettleMaintenanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(second.IsSuccess);
        Assert.Equal(ErrorType.Conflict, second.ErrorType);
        Assert.Single(_billingService.SettledCalls);
    }

    [Fact]
    public async Task Handle_NotCompletedStatus_ReturnsConflict()
    {
        var request = MaintenanceRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Bruit suspect", DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);

        var result = await _handler.Handle(new SettleMaintenanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Empty(_billingService.SettledCalls);
    }

    [Fact]
    public async Task Handle_NothingToSettle_ReturnsValidationError()
    {
        var request = MaintenanceRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Bruit suspect", DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);
        await _requestRepository.TryTransitionAsync(
            request.Id, [MaintenanceRequestStatus.PendingGarageResponse], MaintenanceRequestStatus.Completed, null, null, null, null, null, null,
            DateTime.UtcNow, CancellationToken.None); // Completed with no FinalCost set

        var result = await _handler.Handle(new SettleMaintenanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    /// <summary>The core scenario: the ledger entry was already posted (as if PostBatchAsync had committed in a prior, crashed attempt) but SettledAtUtc is still null. The retry must converge without ever posting a second ledger entry.</summary>
    [Fact]
    public async Task Handle_OrphanedLedgerEntryFromPriorCrash_ConvergesWithoutDoublePosting()
    {
        var request = await CreateCompletedRequestAsync(100m);
        _billingService.RequestIdsWithOrphanedLedgerEntry.Add(request.Id);

        var result = await _handler.Handle(new SettleMaintenanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_billingService.SettledCalls);
        var reloaded = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.NotNull(reloaded!.SettledAtUtc);
    }

    [Fact]
    public async Task Handle_RequestNotFound_ReturnsNotFound()
    {
        var result = await _handler.Handle(new SettleMaintenanceRequestCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
