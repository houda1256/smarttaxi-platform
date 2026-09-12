using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Payments.Taxes.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class TaxRuleConfiguration : IEntityTypeConfiguration<TaxRule>
{
    public void Configure(EntityTypeBuilder<TaxRule> builder)
    {
        builder.ToTable("TaxRules");

        builder.HasKey(rule => rule.Id);
        builder.Property(rule => rule.Id).ValueGeneratedNever();

        builder.Property(rule => rule.TaxName).HasMaxLength(200).IsRequired();
        builder.Property(rule => rule.TaxRate).HasPrecision(5, 2).IsRequired();
        builder.Property(rule => rule.Jurisdiction).HasMaxLength(100).IsRequired();
        builder.Property(rule => rule.ApplicableService).HasMaxLength(100).IsRequired();
        builder.HasIndex(rule => rule.ApplicableService);

        builder.Property(rule => rule.EffectiveFrom).IsRequired();
        builder.Property(rule => rule.EffectiveTo);
        builder.Property(rule => rule.IsActive).IsRequired();
        builder.HasIndex(rule => rule.IsActive);

        builder.Property(rule => rule.ExemptionRules).HasMaxLength(1000);

        builder.Property(rule => rule.CreatedAt).IsRequired();
        builder.Property(rule => rule.UpdatedAt).IsRequired();
    }
}
