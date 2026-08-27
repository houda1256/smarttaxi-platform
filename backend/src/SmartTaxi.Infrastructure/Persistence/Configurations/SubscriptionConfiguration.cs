using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Subscriptions.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");

        builder.HasKey(subscription => subscription.Id);
        builder.Property(subscription => subscription.Id).ValueGeneratedNever();

        builder.Property(subscription => subscription.SubscriberId).IsRequired();
        builder.Property(subscription => subscription.PlanId).IsRequired();
        builder.HasIndex(subscription => subscription.PlanId);

        builder.Property(subscription => subscription.TargetRole).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(subscription => subscription.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(subscription => subscription.StartDate).IsRequired();
        builder.Property(subscription => subscription.EndDate).IsRequired();
        builder.Property(subscription => subscription.TrialEndsAt);
        builder.Property(subscription => subscription.AutoRenew).IsRequired();

        builder.Property(subscription => subscription.CreatedAt).IsRequired();
        builder.Property(subscription => subscription.UpdatedAt).IsRequired();

        // "No two concurrent Pending/Active subscriptions for the same role"
        // enforced atomically at the DB level, not just in the Application layer.
        builder.HasIndex(subscription => new { subscription.SubscriberId, subscription.TargetRole })
            .IsUnique()
            .HasFilter("\"Status\" IN ('Pending', 'Active')");

        builder.HasIndex(subscription => subscription.Status);

        // Module 12 (Analytics) — SubscriptionGrowthCount filters by CreatedAt alone.
        builder.HasIndex(subscription => subscription.CreatedAt);
    }
}
