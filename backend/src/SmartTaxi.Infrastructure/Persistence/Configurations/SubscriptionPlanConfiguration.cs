using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Subscriptions.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("SubscriptionPlans");

        builder.HasKey(plan => plan.Id);
        builder.Property(plan => plan.Id).ValueGeneratedNever();

        builder.Property(plan => plan.Name).HasMaxLength(200).IsRequired();
        builder.Property(plan => plan.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(plan => plan.Code).IsUnique();

        builder.Property(plan => plan.Description).HasMaxLength(2000).IsRequired();
        builder.Property(plan => plan.TargetRole).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.HasIndex(plan => plan.TargetRole);

        builder.Property(plan => plan.Price).HasPrecision(18, 2).IsRequired();
        builder.Property(plan => plan.Currency).HasMaxLength(3).IsRequired();
        builder.Property(plan => plan.BillingPeriod).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(plan => plan.TrialPeriodDays).IsRequired();

        // Reassigned wholesale (never mutated in place) by Create/UpdateDetails, so
        // reference-based change tracking is enough — no ValueComparer needed.
        builder.Property(plan => plan.Features)
            .HasConversion(
                features => string.Join('|', features),
                value => value.Length == 0
                    ? new List<string>()
                    : value.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList())
            .HasColumnType("text");

        builder.Property(plan => plan.Limits)
            .HasConversion(
                limits => JsonSerializer.Serialize(limits, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<Dictionary<string, int>>(json, (JsonSerializerOptions?)null) ?? new Dictionary<string, int>())
            .HasColumnType("text");

        builder.Property(plan => plan.IsActive).IsRequired();
        builder.HasIndex(plan => plan.IsActive);

        builder.Property(plan => plan.CreatedAt).IsRequired();
        builder.Property(plan => plan.UpdatedAt).IsRequired();
    }
}
