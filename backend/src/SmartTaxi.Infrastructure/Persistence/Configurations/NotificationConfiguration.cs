using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Notifications.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(notification => notification.Id);
        builder.Property(notification => notification.Id).ValueGeneratedNever();

        builder.Property(notification => notification.RecipientUserId).IsRequired();
        builder.Property(notification => notification.Category).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(notification => notification.TemplateKey).HasMaxLength(200).IsRequired();
        builder.Property(notification => notification.Title).HasMaxLength(500).IsRequired();
        builder.Property(notification => notification.Body).HasMaxLength(4000).IsRequired();

        // Reassigned wholesale by Create (never mutated in place) — reference-based change tracking is enough, same convention as SubscriptionPlan.Limits.
        builder.Property(notification => notification.Variables)
            .HasConversion(
                variables => JsonSerializer.Serialize(variables, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>())
            .HasColumnType("text");

        builder.Property(notification => notification.IsMandatory).IsRequired();
        builder.Property(notification => notification.SourceType).HasMaxLength(100).IsRequired();
        builder.Property(notification => notification.SourceId).IsRequired();

        builder.Property(notification => notification.IdempotencyKey).HasMaxLength(400).IsRequired();
        builder.HasIndex(notification => notification.IdempotencyKey).IsUnique();

        builder.Property(notification => notification.CreatedAtUtc).IsRequired();
        builder.Property(notification => notification.ReadAtUtc);

        builder.HasIndex(notification => new { notification.RecipientUserId, notification.CreatedAtUtc });

        // Fast "unread count"/"unread list" lookups without scanning read notifications.
        builder.HasIndex(notification => notification.RecipientUserId)
            .HasDatabaseName("IX_Notifications_RecipientUserId_Unread")
            .HasFilter("\"ReadAtUtc\" IS NULL");
    }
}
