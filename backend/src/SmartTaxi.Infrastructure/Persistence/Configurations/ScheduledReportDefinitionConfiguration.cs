using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Analytics.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class ScheduledReportDefinitionConfiguration : IEntityTypeConfiguration<ScheduledReportDefinition>
{
    public void Configure(EntityTypeBuilder<ScheduledReportDefinition> builder)
    {
        builder.ToTable("ScheduledReportDefinitions");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.Category).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(d => d.Frequency).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(d => d.RecipientUserId).IsRequired();
        builder.HasIndex(d => d.RecipientUserId);

        builder.Property(d => d.IsActive).IsRequired();

        builder.Property(d => d.NextRunAtUtc).IsRequired();
        builder.Property(d => d.LastProcessedAtUtc);
        builder.Property(d => d.ProcessingClaimedAtUtc);

        // The exact predicate ProcessDueScheduledReportsCommand's GetDueIdsAsync/TryClaimAsync filter on.
        builder.HasIndex(d => new { d.IsActive, d.NextRunAtUtc }).HasDatabaseName("IX_ScheduledReportDefinitions_IsActive_NextRunAtUtc");

        builder.Property(d => d.CreatedAtUtc).IsRequired();
        builder.Property(d => d.UpdatedAtUtc).IsRequired();
    }
}
