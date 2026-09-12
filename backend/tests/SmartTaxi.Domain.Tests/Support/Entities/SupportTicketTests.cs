using SmartTaxi.Domain.Support.Entities;
using SmartTaxi.Domain.Support.Enums;
using SmartTaxi.Domain.Support.Events;

namespace SmartTaxi.Domain.Tests.Support.Entities;

public class SupportTicketTests
{
    private static SupportTicket CreateValid() =>
        SupportTicket.Create(
            Guid.NewGuid(), SupportTicketCategory.Billing, "Facture incorrecte", "Le montant prélevé ne correspond pas à la course.",
            SupportTicketPriority.Medium, null, null, DateTime.UtcNow);

    [Fact]
    public void Create_WithValidFields_StartsAtOpenAndRaisesEvent()
    {
        var ticket = CreateValid();

        Assert.Equal(SupportTicketStatus.Open, ticket.Status);
        var raised = Assert.Single(ticket.DomainEvents);
        Assert.IsType<SupportTicketCreated>(raised);
        Assert.StartsWith("SUP-", ticket.TicketNumber);
        Assert.Null(ticket.AssignedAdminUserId);
        Assert.Null(ticket.EscalatedIncidentId);
    }

    [Fact]
    public void Create_GeneratesUniqueTicketNumbersEvenAtTheSameInstant()
    {
        var now = DateTime.UtcNow;
        var first = SupportTicket.Create(
            Guid.NewGuid(), SupportTicketCategory.Other, "Sujet", "Description", SupportTicketPriority.Low, null, null, now);
        var second = SupportTicket.Create(
            Guid.NewGuid(), SupportTicketCategory.Other, "Sujet", "Description", SupportTicketPriority.Low, null, null, now);

        Assert.NotEqual(first.TicketNumber, second.TicketNumber);
    }

    [Fact]
    public void Create_WithEmptyRequesterUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            SupportTicket.Create(Guid.Empty, SupportTicketCategory.Other, "Sujet", "Description", SupportTicketPriority.Low, null, null, DateTime.UtcNow));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingSubject_Throws(string subject)
    {
        Assert.Throws<ArgumentException>(() =>
            SupportTicket.Create(Guid.NewGuid(), SupportTicketCategory.Other, subject, "Description", SupportTicketPriority.Low, null, null, DateTime.UtcNow));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingDescription_Throws(string description)
    {
        Assert.Throws<ArgumentException>(() =>
            SupportTicket.Create(Guid.NewGuid(), SupportTicketCategory.Other, "Sujet", description, SupportTicketPriority.Low, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithRelatedEntityTypeButNoId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            SupportTicket.Create(
                Guid.NewGuid(), SupportTicketCategory.RideIssue, "Sujet", "Description", SupportTicketPriority.Low,
                Domain.Support.Enums.SupportRelatedEntityType.Ride, null, DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithRelatedEntityIdButNoType_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            SupportTicket.Create(
                Guid.NewGuid(), SupportTicketCategory.RideIssue, "Sujet", "Description", SupportTicketPriority.Low, null, Guid.NewGuid(),
                DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithBothRelatedEntityFields_Succeeds()
    {
        var rideId = Guid.NewGuid();
        var ticket = SupportTicket.Create(
            Guid.NewGuid(), SupportTicketCategory.RideIssue, "Sujet", "Description", SupportTicketPriority.High,
            Domain.Support.Enums.SupportRelatedEntityType.Ride, rideId, DateTime.UtcNow);

        Assert.Equal(Domain.Support.Enums.SupportRelatedEntityType.Ride, ticket.RelatedEntityType);
        Assert.Equal(rideId, ticket.RelatedEntityId);
    }
}
