using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Support.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class SupportIncidentConfiguration : IEntityTypeConfiguration<SupportIncident>
{
    public void Configure(EntityTypeBuilder<SupportIncident> builder)
    {
        builder.ToTable("SupportIncidents");

        builder.HasKey(incident => incident.Id);
        builder.Property(incident => incident.Id).ValueGeneratedNever();

        builder.Property(incident => incident.IncidentNumber).HasMaxLength(40).IsRequired();
        builder.HasIndex(incident => incident.IncidentNumber).IsUnique();

        builder.Property(incident => incident.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(incident => incident.Severity).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(incident => incident.Title).HasMaxLength(200).IsRequired();
        builder.Property(incident => incident.Description).HasMaxLength(4000).IsRequired();
        builder.Property(incident => incident.ReportedByUserId);

        builder.Property(incident => incident.RelatedEntityType).HasConversion<string>().HasMaxLength(40);
        builder.Property(incident => incident.RelatedEntityId);

        builder.Property(incident => incident.Latitude);
        builder.Property(incident => incident.Longitude);
        builder.Property(incident => incident.OccurredAtUtc).IsRequired();

        builder.Property(incident => incident.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(incident => incident.Status);

        builder.Property(incident => incident.AssignedAdminUserId);
        builder.HasIndex(incident => incident.AssignedAdminUserId);

        builder.Property(incident => incident.Resolution).HasMaxLength(4000);

        builder.Property(incident => incident.SourceType).HasMaxLength(40);
        builder.Property(incident => incident.SourceId);

        // Idempotency anchor for ISupportIncidentReporter: at most one incident per (SourceType, SourceId).
        // Manually created incidents have SourceType == NULL and are excluded from the filter.
        builder.HasIndex(incident => new { incident.SourceType, incident.SourceId })
            .IsUnique()
            .HasDatabaseName("IX_SupportIncidents_Source_Unique")
            .HasFilter("\"SourceType\" IS NOT NULL");

        builder.Property(incident => incident.CreatedAtUtc).IsRequired();
        builder.Property(incident => incident.UpdatedAtUtc).IsRequired();
        builder.Property(incident => incident.ResolvedAtUtc);
        builder.Property(incident => incident.ClosedAtUtc);
    }
}
