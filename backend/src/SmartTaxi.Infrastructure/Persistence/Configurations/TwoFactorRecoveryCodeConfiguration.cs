using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Identity.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class TwoFactorRecoveryCodeConfiguration : IEntityTypeConfiguration<TwoFactorRecoveryCode>
{
    public void Configure(EntityTypeBuilder<TwoFactorRecoveryCode> builder)
    {
        builder.ToTable("TwoFactorRecoveryCodes");

        builder.HasKey(code => code.Id);
        builder.Property(code => code.Id).ValueGeneratedNever();

        builder.Property(code => code.UserId).IsRequired();
        builder.HasIndex(code => code.UserId);

        builder.Property(code => code.CodeHash).HasMaxLength(500).IsRequired();

        builder.Property(code => code.CreatedAt).IsRequired();
    }
}
