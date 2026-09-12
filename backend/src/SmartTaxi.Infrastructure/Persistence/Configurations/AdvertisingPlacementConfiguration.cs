using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class AdvertisingPlacementConfiguration : IEntityTypeConfiguration<AdvertisingPlacement>
{
    public void Configure(EntityTypeBuilder<AdvertisingPlacement> builder)
    {
        builder.ToTable("AdvertisingPlacements");

        builder.HasKey(placement => placement.Id);
        builder.Property(placement => placement.Id).ValueGeneratedNever();

        builder.Property(placement => placement.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(placement => placement.Code).IsUnique();

        builder.Property(placement => placement.Name).HasMaxLength(200).IsRequired();
        builder.Property(placement => placement.Description).HasMaxLength(2000);

        // Reassigned wholesale (never mutated in place) — same convention as LoyaltyReward.TargetRoles.
        builder.Property(placement => placement.SupportedMediaTypes)
            .HasConversion(
                types => string.Join('|', types),
                value => value.Length == 0
                    ? new List<AdMediaType>()
                    : value.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(Enum.Parse<AdMediaType>).ToList())
            .HasColumnType("text");

        builder.Property(placement => placement.IsActive).IsRequired();
        builder.HasIndex(placement => placement.IsActive);
        builder.Property(placement => placement.CreatedAtUtc).IsRequired();
        builder.Property(placement => placement.UpdatedAtUtc).IsRequired();
    }
}
