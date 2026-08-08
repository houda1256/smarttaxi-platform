using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Payments.CashRegister.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class CashRegisterBoxConfiguration : IEntityTypeConfiguration<CashRegisterBox>
{
    public void Configure(EntityTypeBuilder<CashRegisterBox> builder)
    {
        builder.ToTable("CashRegisters");

        builder.HasKey(cashRegister => cashRegister.Id);
        builder.Property(cashRegister => cashRegister.Id).ValueGeneratedNever();

        builder.Property(cashRegister => cashRegister.OwnerId).IsRequired();
        builder.HasIndex(cashRegister => cashRegister.OwnerId);

        builder.Property(cashRegister => cashRegister.Label).HasMaxLength(200).IsRequired();
        builder.Property(cashRegister => cashRegister.CreatedAt).IsRequired();
    }
}
