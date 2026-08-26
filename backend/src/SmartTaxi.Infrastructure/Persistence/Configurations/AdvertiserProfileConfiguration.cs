using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class AdvertiserProfileConfiguration : IEntityTypeConfiguration<AdvertiserProfile>
{
    public void Configure(EntityTypeBuilder<AdvertiserProfile> builder)
    {
        builder.ToTable("AdvertiserProfiles");

        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.Id).ValueGeneratedNever();

        builder.Property(profile => profile.UserId).IsRequired();
        builder.HasIndex(profile => profile.UserId).IsUnique();

        builder.Property(profile => profile.BusinessName).HasMaxLength(200).IsRequired();
        builder.Property(profile => profile.LegalName).HasMaxLength(200).IsRequired();
        builder.Property(profile => profile.TaxIdentifier).HasMaxLength(100).IsRequired();
        builder.Property(profile => profile.City).HasMaxLength(100).IsRequired();
        builder.Property(profile => profile.Address).HasMaxLength(500);
        builder.Property(profile => profile.ContactEmail).HasMaxLength(320).IsRequired();
        builder.Property(profile => profile.ContactPhone).HasMaxLength(30);
        builder.Property(profile => profile.IsActive).IsRequired();
        builder.Property(profile => profile.CreatedAtUtc).IsRequired();
        builder.Property(profile => profile.UpdatedAtUtc).IsRequired();
    }
}
