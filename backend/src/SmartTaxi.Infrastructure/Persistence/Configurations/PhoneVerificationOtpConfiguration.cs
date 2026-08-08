using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Identity.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class PhoneVerificationOtpConfiguration : IEntityTypeConfiguration<PhoneVerificationOtp>
{
    public void Configure(EntityTypeBuilder<PhoneVerificationOtp> builder)
    {
        builder.ToTable("PhoneVerificationOtps");

        builder.HasKey(otp => otp.Id);
        builder.Property(otp => otp.Id).ValueGeneratedNever();

        builder.Property(otp => otp.UserId).IsRequired();
        builder.HasIndex(otp => otp.UserId);

        builder.Property(otp => otp.OtpHash).HasMaxLength(500).IsRequired();

        builder.Property(otp => otp.CreatedAt).IsRequired();
        builder.Property(otp => otp.ExpiresAt).IsRequired();
        builder.Property(otp => otp.AttemptCount).IsRequired().HasDefaultValue(0);
    }
}
