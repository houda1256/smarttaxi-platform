using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Support.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.ToTable("SupportTickets");

        builder.HasKey(ticket => ticket.Id);
        builder.Property(ticket => ticket.Id).ValueGeneratedNever();

        builder.Property(ticket => ticket.TicketNumber).HasMaxLength(40).IsRequired();
        builder.HasIndex(ticket => ticket.TicketNumber).IsUnique();

        builder.Property(ticket => ticket.RequesterUserId).IsRequired();
        builder.HasIndex(ticket => ticket.RequesterUserId);

        builder.Property(ticket => ticket.Category).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(ticket => ticket.Subject).HasMaxLength(200).IsRequired();
        builder.Property(ticket => ticket.Description).HasMaxLength(4000).IsRequired();
        builder.Property(ticket => ticket.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(ticket => ticket.RelatedEntityType).HasConversion<string>().HasMaxLength(40);
        builder.Property(ticket => ticket.RelatedEntityId);

        builder.Property(ticket => ticket.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.HasIndex(ticket => ticket.Status);

        builder.Property(ticket => ticket.AssignedAdminUserId);
        builder.HasIndex(ticket => ticket.AssignedAdminUserId);

        builder.Property(ticket => ticket.CreatedAtUtc).IsRequired();
        // Module 12 (Analytics) — SupportTicketGrowthCount filters by CreatedAtUtc alone (never combined
        // with Status in the same query), so a single-column index serves it better than a composite.
        builder.HasIndex(ticket => ticket.CreatedAtUtc);
        builder.Property(ticket => ticket.UpdatedAtUtc).IsRequired();
        builder.Property(ticket => ticket.ResolvedAtUtc);
        builder.Property(ticket => ticket.ClosedAtUtc);
        builder.Property(ticket => ticket.ReopenedAtUtc);
        builder.Property(ticket => ticket.Resolution).HasMaxLength(4000);

        builder.Property(ticket => ticket.EscalatedIncidentId);
    }
}
