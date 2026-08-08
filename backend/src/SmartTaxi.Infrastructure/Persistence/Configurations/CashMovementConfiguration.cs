using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Payments.CashRegister.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class CashMovementConfiguration : IEntityTypeConfiguration<CashMovement>
{
    public void Configure(EntityTypeBuilder<CashMovement> builder)
    {
        builder.ToTable("CashMovements");

        builder.HasKey(movement => movement.Id);
        builder.Property(movement => movement.Id).ValueGeneratedNever();

        builder.Property(movement => movement.CashRegisterSessionId).IsRequired();
        builder.HasIndex(movement => movement.CashRegisterSessionId);

        builder.Property(movement => movement.MovementType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(movement => movement.Amount).HasPrecision(10, 2).IsRequired();
        builder.Property(movement => movement.Description).HasMaxLength(1000);
        builder.Property(movement => movement.RecordedAt).IsRequired();
        builder.Property(movement => movement.RecordedBy).IsRequired();
    }
}
