using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Notifications.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class NotificationDeliveryAttemptConfiguration : IEntityTypeConfiguration<NotificationDeliveryAttempt>
{
    public void Configure(EntityTypeBuilder<NotificationDeliveryAttempt> builder)
    {
        builder.ToTable("NotificationDeliveryAttempts");

        builder.HasKey(attempt => attempt.Id);
        builder.Property(attempt => attempt.Id).ValueGeneratedNever();

        builder.Property(attempt => attempt.NotificationId).IsRequired();
        builder.HasIndex(attempt => attempt.NotificationId);

        builder.Property(attempt => attempt.Channel).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(attempt => attempt.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(attempt => attempt.AttemptNumber).IsRequired();
        builder.Property(attempt => attempt.AttemptedAtUtc).IsRequired();
        builder.Property(attempt => attempt.SentAtUtc);
        builder.Property(attempt => attempt.DeliveredAtUtc);
        builder.Property(attempt => attempt.FailureCode).HasMaxLength(50);
        builder.Property(attempt => attempt.FailureMessage).HasMaxLength(500);
        builder.Property(attempt => attempt.ProviderMessageId).HasMaxLength(200);
        builder.Property(attempt => attempt.NextAttemptAtUtc);

        // The retry sweep's own filter (Status = Failed AND NextAttemptAtUtc <= now) stays translatable to SQL.
        builder.HasIndex(attempt => new { attempt.Status, attempt.NextAttemptAtUtc });
    }
}
