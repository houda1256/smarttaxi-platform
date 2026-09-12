using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Notifications.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class ScheduledNotificationConfiguration : IEntityTypeConfiguration<ScheduledNotification>
{
    public void Configure(EntityTypeBuilder<ScheduledNotification> builder)
    {
        builder.ToTable("ScheduledNotifications");

        builder.HasKey(scheduled => scheduled.Id);
        builder.Property(scheduled => scheduled.Id).ValueGeneratedNever();

        builder.Property(scheduled => scheduled.RecipientUserId).IsRequired();
        builder.Property(scheduled => scheduled.Category).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(scheduled => scheduled.TemplateKey).HasMaxLength(200).IsRequired();

        builder.Property(scheduled => scheduled.Variables)
            .HasConversion(
                variables => JsonSerializer.Serialize(variables, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>())
            .HasColumnType("text");

        builder.Property(scheduled => scheduled.IsMandatory).IsRequired();
        builder.Property(scheduled => scheduled.SourceType).HasMaxLength(100).IsRequired();
        builder.Property(scheduled => scheduled.SourceId).IsRequired();

        builder.Property(scheduled => scheduled.IdempotencyKey).HasMaxLength(400).IsRequired();
        builder.HasIndex(scheduled => scheduled.IdempotencyKey).IsUnique();

        builder.Property(scheduled => scheduled.ScheduledAtUtc).IsRequired();
        builder.Property(scheduled => scheduled.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(scheduled => scheduled.ProcessedAtUtc);
        builder.Property(scheduled => scheduled.CreatedAtUtc).IsRequired();

        // The due-sweep's own filter (Status = Pending AND ScheduledAtUtc <= now) stays translatable to SQL.
        builder.HasIndex(scheduled => new { scheduled.Status, scheduled.ScheduledAtUtc });
    }
}
