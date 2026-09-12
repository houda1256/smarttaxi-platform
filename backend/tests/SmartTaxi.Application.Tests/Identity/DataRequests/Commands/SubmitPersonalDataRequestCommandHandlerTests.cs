using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.DataRequests.Commands.SubmitPersonalDataRequest;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.DataRequests.Enums;

namespace SmartTaxi.Application.Tests.Identity.DataRequests.Commands;

public class SubmitPersonalDataRequestCommandHandlerTests
{
    private readonly FakePersonalDataRequestRepository _repository = new();
    private readonly SubmitPersonalDataRequestCommandHandler _handler;

    public SubmitPersonalDataRequestCommandHandlerTests()
    {
        _handler = new SubmitPersonalDataRequestCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_ForNewRequest_CreatesPendingRequest()
    {
        var result = await _handler.Handle(
            new SubmitPersonalDataRequestCommand(Guid.NewGuid(), PersonalDataRequestType.Export), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _repository.Count);
    }

    [Fact]
    public async Task Handle_WithAlreadyPendingRequestOfSameType_ReturnsConflict()
    {
        var userId = Guid.NewGuid();
        await _handler.Handle(new SubmitPersonalDataRequestCommand(userId, PersonalDataRequestType.Export), CancellationToken.None);

        var result = await _handler.Handle(
            new SubmitPersonalDataRequestCommand(userId, PersonalDataRequestType.Export), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(1, _repository.Count);
    }

    [Fact]
    public async Task Handle_WithPendingRequestOfDifferentType_Succeeds()
    {
        var userId = Guid.NewGuid();
        await _handler.Handle(new SubmitPersonalDataRequestCommand(userId, PersonalDataRequestType.Export), CancellationToken.None);

        var result = await _handler.Handle(
            new SubmitPersonalDataRequestCommand(userId, PersonalDataRequestType.Anonymization), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, _repository.Count);
    }
}
