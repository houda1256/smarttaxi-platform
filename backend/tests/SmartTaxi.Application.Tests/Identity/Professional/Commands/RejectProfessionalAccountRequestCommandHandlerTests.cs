using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Professional.Commands.RejectProfessionalAccountRequest;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.Professional.Entities;
using SmartTaxi.Domain.Identity.Professional.Enums;

namespace SmartTaxi.Application.Tests.Identity.Professional.Commands;

public class RejectProfessionalAccountRequestCommandHandlerTests
{
    private readonly FakeProfessionalAccountRequestRepository _repository = new();
    private readonly RejectProfessionalAccountRequestCommandHandler _handler;

    public RejectProfessionalAccountRequestCommandHandlerTests()
    {
        _handler = new RejectProfessionalAccountRequestCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_WithReason_RejectsPendingRequest()
    {
        var request = new ProfessionalAccountRequest(Guid.NewGuid(), UserRole.Driver, DateTime.UtcNow);
        await _repository.AddAsync(request, CancellationToken.None);

        var result = await _handler.Handle(
            new RejectProfessionalAccountRequestCommand(Guid.NewGuid(), request.Id, "Documents illisibles", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _repository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(ProfessionalAccountStatus.Rejected, reloaded!.Status);
        Assert.Equal("Documents illisibles", reloaded.RejectionReason);
    }

    [Fact]
    public async Task Handle_WithEmptyReason_ReturnsValidationError()
    {
        var request = new ProfessionalAccountRequest(Guid.NewGuid(), UserRole.Driver, DateTime.UtcNow);
        await _repository.AddAsync(request, CancellationToken.None);

        var result = await _handler.Handle(
            new RejectProfessionalAccountRequestCommand(Guid.NewGuid(), request.Id, "  ", null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }
}
