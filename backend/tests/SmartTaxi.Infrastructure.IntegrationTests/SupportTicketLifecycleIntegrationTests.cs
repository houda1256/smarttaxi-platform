using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Support;
using SmartTaxi.Domain.Support.Entities;
using SmartTaxi.Domain.Support.Enums;
using SmartTaxi.Infrastructure.Support.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>Proves, against real PostgreSQL, the SupportTicket concurrency/atomicity guarantees that a real ExecuteUpdateAsync/transaction needs an actual database to validate (an in-memory fake cannot exercise Postgres's own row-locking/unique-index behavior).</summary>
[Collection("SharedPostgres")]
public class SupportTicketLifecycleIntegrationTests
{
    private readonly SharedPostgresFixture _fixture;

    public SupportTicketLifecycleIntegrationTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<SupportTicket> CreateTicketAsync(Guid requesterUserId)
    {
        var ticket = SupportTicket.Create(
            requesterUserId, SupportTicketCategory.Other, "Sujet", "Description", SupportTicketPriority.Low, null, null, DateTime.UtcNow);

        await using var context = _fixture.CreateContext();
        await new SupportTicketRepository(context).TryAddAsync(ticket, CancellationToken.None);
        return ticket;
    }

    [Fact]
    public async Task TwoConcurrentAssignments_OnlyOneSucceeds()
    {
        var ticket = await CreateTicketAsync(Guid.NewGuid());

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new SupportTicketRepository(context1).TryAssignAsync(ticket.Id, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None),
            new SupportTicketRepository(context2).TryAssignAsync(ticket.Id, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, r => r);
        Assert.Single(results, r => !r);
    }

    [Fact]
    public async Task RequesterMessage_WhileWaitingForCustomer_AtomicallyAdvancesToInProgress()
    {
        var requesterUserId = Guid.NewGuid();
        var ticket = await CreateTicketAsync(requesterUserId);
        var adminUserId = Guid.NewGuid();

        await using (var context = _fixture.CreateContext())
        {
            var repository = new SupportTicketRepository(context);
            await repository.TryAssignAsync(ticket.Id, adminUserId, DateTime.UtcNow, CancellationToken.None);
            await repository.TryTransitionAsync(
                ticket.Id, [SupportTicketStatus.Assigned], SupportTicketStatus.InProgress, adminUserId, null, DateTime.UtcNow,
                CancellationToken.None);
            await repository.TryTransitionAsync(
                ticket.Id, [SupportTicketStatus.InProgress], SupportTicketStatus.WaitingForCustomer, adminUserId, null, DateTime.UtcNow,
                CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var advanced = await new SupportTicketRepository(context).TryAddRequesterMessageAndAdvanceAsync(
                ticket.Id, requesterUserId, "Voici la précision demandée", DateTime.UtcNow, CancellationToken.None);
            Assert.True(advanced);
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await readContext.SupportTickets.FirstAsync(t => t.Id == ticket.Id, CancellationToken.None);
        Assert.Equal(SupportTicketStatus.InProgress, reloaded.Status);
        var messages = await readContext.SupportTicketMessages.Where(m => m.TicketId == ticket.Id).ToListAsync(CancellationToken.None);
        Assert.Single(messages);
    }

    [Fact]
    public async Task InternalNote_NeverReturnedByRequesterFacingQuery()
    {
        var ticket = await CreateTicketAsync(Guid.NewGuid());
        var adminUserId = Guid.NewGuid();

        await using (var context = _fixture.CreateContext())
        {
            var messageRepository = new SupportTicketMessageRepository(context);
            await messageRepository.AddAsync(
                SupportTicketMessage.Create(ticket.Id, adminUserId, "Réponse visible", isInternalNote: false, DateTime.UtcNow),
                CancellationToken.None);
            await messageRepository.AddAsync(
                SupportTicketMessage.Create(ticket.Id, adminUserId, "Note interne sensible", isInternalNote: true, DateTime.UtcNow),
                CancellationToken.None);
        }

        await using var context2 = _fixture.CreateContext();
        var messageRepository2 = new SupportTicketMessageRepository(context2);
        var visible = await messageRepository2.GetVisibleForRequesterAsync(ticket.Id, CancellationToken.None);
        var all = await messageRepository2.GetAllForAdminAsync(ticket.Id, CancellationToken.None);

        Assert.Single(visible);
        Assert.False(visible.Single().IsInternalNote);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task ConcurrentEscalations_OnlyOneIncidentCreated()
    {
        var ticket = await CreateTicketAsync(Guid.NewGuid());
        var adminUserId = Guid.NewGuid();

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var escalationRepository1 = new SupportTicketEscalationRepository(
            context1, new SupportIncidentReporter(new SupportIncidentRepository(context1)));
        var escalationRepository2 = new SupportTicketEscalationRepository(
            context2, new SupportIncidentReporter(new SupportIncidentRepository(context2)));

        var results = await Task.WhenAll(
            escalationRepository1.TryEscalateAsync(
                ticket.Id, adminUserId, SupportIncidentType.OperationalIncident, SupportIncidentSeverity.Major, null, DateTime.UtcNow,
                CancellationToken.None),
            escalationRepository2.TryEscalateAsync(
                ticket.Id, adminUserId, SupportIncidentType.OperationalIncident, SupportIncidentSeverity.Major, null, DateTime.UtcNow,
                CancellationToken.None));

        Assert.Equal(results[0], results[1]);

        await using var readContext = _fixture.CreateContext();
        var incidentCount = await readContext.SupportIncidents
            .CountAsync(i => i.SourceType == "SupportTicket" && i.SourceId == ticket.Id, CancellationToken.None);
        Assert.Equal(1, incidentCount);

        var reloadedTicket = await readContext.SupportTickets.FirstAsync(t => t.Id == ticket.Id, CancellationToken.None);
        Assert.Equal(results[0], reloadedTicket.EscalatedIncidentId);
    }
}
