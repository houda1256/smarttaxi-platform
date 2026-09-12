using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Payments.CashDeclarations.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class CashDeclarationConfiguration : IEntityTypeConfiguration<CashDeclaration>
{
    public void Configure(EntityTypeBuilder<CashDeclaration> builder)
    {
        builder.ToTable("CashDeclarations");

        builder.HasKey(declaration => declaration.Id);
        builder.Property(declaration => declaration.Id).ValueGeneratedNever();

        builder.Property(declaration => declaration.DriverId).IsRequired();
        builder.HasIndex(declaration => declaration.DriverId);

        builder.Property(declaration => declaration.AssignmentId);
        builder.Property(declaration => declaration.PeriodStart).IsRequired();
        builder.Property(declaration => declaration.PeriodEnd).IsRequired();

        builder.Property(declaration => declaration.ExpectedCash).HasPrecision(10, 2).IsRequired();
        builder.Property(declaration => declaration.DeclaredCash).HasPrecision(10, 2).IsRequired();
        builder.Property(declaration => declaration.Difference).HasPrecision(10, 2).IsRequired();

        builder.Property(declaration => declaration.OperatingModel).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(declaration => declaration.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(declaration => declaration.Status);

        builder.Property(declaration => declaration.SubmittedAt).IsRequired();
        builder.Property(declaration => declaration.ReviewedBy);
        builder.Property(declaration => declaration.ReviewedAt);
    }
}
