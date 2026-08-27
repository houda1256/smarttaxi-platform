using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Administration.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

/// <summary>
/// UPDATE/DELETE on this table are additionally rejected at the database
/// level by a PostgreSQL trigger created in the AddAdministrationModule
/// migration — this configuration only maps columns/indexes.
/// </summary>
public sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).ValueGeneratedNever();

        builder.Property(entry => entry.ActorUserId);
        builder.Property(entry => entry.Action).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(entry => entry.TargetType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(entry => entry.TargetId).IsRequired();
        builder.Property(entry => entry.OccurredAtUtc).IsRequired();
        builder.Property(entry => entry.CorrelationId).HasMaxLength(64);
        builder.Property(entry => entry.IpAddress).HasMaxLength(45);
        builder.Property(entry => entry.UserAgent).HasMaxLength(500);
        builder.Property(entry => entry.Metadata).HasMaxLength(2000);

        builder.HasIndex(entry => new { entry.TargetType, entry.TargetId, entry.OccurredAtUtc });
        builder.HasIndex(entry => entry.ActorUserId);
    }
}
