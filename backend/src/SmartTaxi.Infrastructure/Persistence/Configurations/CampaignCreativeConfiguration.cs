using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class CampaignCreativeConfiguration : IEntityTypeConfiguration<CampaignCreative>
{
    public void Configure(EntityTypeBuilder<CampaignCreative> builder)
    {
        builder.ToTable("AdvertisingCreatives");

        builder.HasKey(creative => creative.Id);
        builder.Property(creative => creative.Id).ValueGeneratedNever();

        builder.Property(creative => creative.CampaignId).IsRequired();
        builder.HasIndex(creative => creative.CampaignId);

        builder.Property(creative => creative.MediaType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(creative => creative.MimeType).HasMaxLength(100).IsRequired();
        builder.Property(creative => creative.DisplayName).HasMaxLength(255).IsRequired();
        builder.Property(creative => creative.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(creative => creative.FileSizeBytes).IsRequired();
        builder.Property(creative => creative.Sha256).HasMaxLength(64).IsRequired();

        builder.Property(creative => creative.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(creative => creative.Version).IsRequired();
        builder.Property(creative => creative.ReplacesCreativeId);
        builder.Property(creative => creative.ReviewedByUserId);
        builder.Property(creative => creative.ReviewedAtUtc);
        builder.Property(creative => creative.RejectionReason).HasMaxLength(1000);
        builder.Property(creative => creative.CreatedAtUtc).IsRequired();
    }
}
