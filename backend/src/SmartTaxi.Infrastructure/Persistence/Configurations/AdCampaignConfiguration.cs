using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class AdCampaignConfiguration : IEntityTypeConfiguration<AdCampaign>
{
    public void Configure(EntityTypeBuilder<AdCampaign> builder)
    {
        builder.ToTable("AdCampaigns");

        builder.HasKey(campaign => campaign.Id);
        builder.Property(campaign => campaign.Id).ValueGeneratedNever();

        builder.Property(campaign => campaign.AdvertiserProfileId).IsRequired();
        builder.Property(campaign => campaign.AdvertiserUserId).IsRequired();
        builder.HasIndex(campaign => campaign.AdvertiserUserId);

        builder.Property(campaign => campaign.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(campaign => campaign.Code).IsUnique();

        builder.Property(campaign => campaign.Name).HasMaxLength(200).IsRequired();
        builder.Property(campaign => campaign.Description).HasMaxLength(2000);
        builder.Property(campaign => campaign.Objective).HasMaxLength(500);

        builder.Property(campaign => campaign.PlacementId).IsRequired();
        builder.Property(campaign => campaign.StartAtUtc).IsRequired();
        builder.Property(campaign => campaign.EndAtUtc).IsRequired();

        builder.Property(campaign => campaign.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.HasIndex(campaign => campaign.Status);
        builder.HasIndex(campaign => new { campaign.Status, campaign.StartAtUtc });
        builder.HasIndex(campaign => new { campaign.Status, campaign.EndAtUtc });

        builder.Property(campaign => campaign.PricingModel).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(campaign => campaign.PriceRate).HasPrecision(12, 3).IsRequired();
        builder.Property(campaign => campaign.BudgetLimit).HasPrecision(12, 3).IsRequired();
        builder.Property(campaign => campaign.ConsumedBudget).HasPrecision(12, 3).IsRequired();
        builder.Property(campaign => campaign.DailyBudgetLimit).HasPrecision(12, 3);
        builder.Property(campaign => campaign.DailyConsumedBudget).HasPrecision(12, 3).IsRequired();
        builder.Property(campaign => campaign.DailyConsumedDateUtc);

        builder.Property(campaign => campaign.TargetCity).HasMaxLength(100);
        builder.Property(campaign => campaign.TargetVehicleCategory).HasMaxLength(50);
        builder.Property(campaign => campaign.TargetDaysOfWeek).HasMaxLength(50);
        builder.Property(campaign => campaign.TargetStartHour);
        builder.Property(campaign => campaign.TargetEndHour);

        builder.Property(campaign => campaign.CreatedAtUtc).IsRequired();
        // Module 12 (Analytics) — CampaignGrowthCount filters by CreatedAtUtc alone.
        builder.HasIndex(campaign => campaign.CreatedAtUtc);
        builder.Property(campaign => campaign.UpdatedAtUtc).IsRequired();
        builder.Property(campaign => campaign.SubmittedAtUtc);
        builder.Property(campaign => campaign.ReviewedAtUtc);
        builder.Property(campaign => campaign.ReviewedByUserId);
        builder.Property(campaign => campaign.ReviewReason).HasMaxLength(1000);
        builder.Property(campaign => campaign.SettledAtUtc);

        builder.ToTable(t => t.HasCheckConstraint("CK_AdCampaigns_BudgetLimit_NonNegative", "\"BudgetLimit\" >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("CK_AdCampaigns_ConsumedBudget_NonNegative", "\"ConsumedBudget\" >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("CK_AdCampaigns_EndAfterStart", "\"EndAtUtc\" > \"StartAtUtc\""));
    }
}
