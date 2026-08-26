using SmartTaxi.Application.Common;
using SmartTaxi.Application.Support.Commands.EscalateTicketToIncident;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Tests.Support.Commands;

public class EscalateTicketToIncidentCommandHandlerTests
{
    [Fact]
    public async Task Handle_ExistingTicket_ReturnsIncidentId()
    {
        var escalationRepository = new FakeSupportTicketEscalationRepository();
        var handler = new EscalateTicketToIncidentCommandHandler(escalationRepository);

        var result = await handler.Handle(
            new EscalateTicketToIncidentCommand(
                Guid.NewGuid(), Guid.NewGuid(), SupportIncidentType.OperationalIncident, SupportIncidentSeverity.Major, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
    }

    [Fact]
    public async Task Handle_UnknownTicket_ReturnsNotFound()
    {
        var escalationRepository = new FakeSupportTicketEscalationRepository { TicketExists = false };
        var handler = new EscalateTicketToIncidentCommandHandler(escalationRepository);

        var result = await handler.Handle(
            new EscalateTicketToIncidentCommand(
                Guid.NewGuid(), Guid.NewGuid(), SupportIncidentType.OperationalIncident, SupportIncidentSeverity.Major, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Handle_RetryAfterEscalation_ReturnsSameIncidentId()
    {
        var escalationRepository = new FakeSupportTicketEscalationRepository();
        var handler = new EscalateTicketToIncidentCommandHandler(escalationRepository);
        var ticketId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        var first = await handler.Handle(
            new EscalateTicketToIncidentCommand(
                ticketId, adminId, SupportIncidentType.OperationalIncident, SupportIncidentSeverity.Major, null),
            CancellationToken.None);
        var second = await handler.Handle(
            new EscalateTicketToIncidentCommand(
                ticketId, adminId, SupportIncidentType.OperationalIncident, SupportIncidentSeverity.Major, null),
            CancellationToken.None);

        Assert.Equal(first.Value, second.Value);
    }
}
