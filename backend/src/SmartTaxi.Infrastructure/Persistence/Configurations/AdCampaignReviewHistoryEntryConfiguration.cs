using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class AdCampaignReviewHistoryEntryConfiguration : IEntityTypeConfiguration<AdCampaignReviewHistoryEntry>
{
    public void Configure(EntityTypeBuilder<AdCampaignReviewHistoryEntry> builder)
    {
        builder.ToTable("AdvertisingCampaignReviewHistory");

        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).ValueGeneratedNever();

        builder.Property(entry => entry.CampaignId).IsRequired();
        builder.HasIndex(entry => new { entry.CampaignId, entry.CreatedAtUtc });

        builder.Property(entry => entry.ReviewerUserId);
        builder.Property(entry => entry.Action).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(entry => entry.PreviousStatus).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(entry => entry.NewStatus).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(entry => entry.Reason).HasMaxLength(1000);
        builder.Property(entry => entry.CreatedAtUtc).IsRequired();
    }
}
