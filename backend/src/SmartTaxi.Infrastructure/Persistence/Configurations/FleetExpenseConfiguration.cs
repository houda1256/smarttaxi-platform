using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Fleet.Expenses.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class FleetExpenseConfiguration : IEntityTypeConfiguration<FleetExpense>
{
    public void Configure(EntityTypeBuilder<FleetExpense> builder)
    {
        builder.ToTable("FleetExpenses");

        builder.HasKey(expense => expense.Id);
        builder.Property(expense => expense.Id).ValueGeneratedNever();

        builder.Property(expense => expense.OwnerId).IsRequired();
        builder.HasIndex(expense => expense.OwnerId);

        builder.Property(expense => expense.FleetId);
        builder.Property(expense => expense.VehicleId);
        builder.Property(expense => expense.DriverId);
        builder.HasIndex(expense => expense.VehicleId);
        builder.HasIndex(expense => expense.DriverId);

        builder.Property(expense => expense.Category).HasConversion<string>().HasMaxLength(30).IsRequired();

        // OwnsOne is safe here for the same reason as TaxiOwnerProfile.Address: FleetExpense's
        // parameterized constructor's "utcNow" parameter has no matching property, so EF
        // materializes it via the parameterless constructor and never needs Money as a ctor arg.
        builder.OwnsOne(expense => expense.Money, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Amount").HasPrecision(12, 2).IsRequired();
            money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });

        builder.Property(expense => expense.ExpenseDate).IsRequired();
        builder.Property(expense => expense.Description).HasMaxLength(1000);
        builder.Property(expense => expense.ReceiptReference).HasMaxLength(500);

        builder.Property(expense => expense.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(expense => expense.CreatedBy).IsRequired();
        builder.Property(expense => expense.CreatedAt).IsRequired();

        builder.HasIndex(expense => expense.Status);
        builder.HasIndex(expense => expense.ExpenseDate);
    }
}
