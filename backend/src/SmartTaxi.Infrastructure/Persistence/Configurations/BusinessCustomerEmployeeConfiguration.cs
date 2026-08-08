using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Payments.BusinessCustomers.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class BusinessCustomerEmployeeConfiguration : IEntityTypeConfiguration<BusinessCustomerEmployee>
{
    public void Configure(EntityTypeBuilder<BusinessCustomerEmployee> builder)
    {
        builder.ToTable("BusinessCustomerEmployees");

        builder.HasKey(employee => employee.Id);
        builder.Property(employee => employee.Id).ValueGeneratedNever();

        builder.Property(employee => employee.BusinessCustomerId).IsRequired();
        builder.HasIndex(employee => employee.BusinessCustomerId);

        builder.Property(employee => employee.UserId).IsRequired();
        builder.HasIndex(employee => employee.UserId);

        builder.Property(employee => employee.Role).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(employee => employee.RideBudgetPerMonth).HasPrecision(10, 2);
        builder.Property(employee => employee.AllowedVehicleCategories).HasMaxLength(500);
        builder.Property(employee => employee.AllowedScheduleStart);
        builder.Property(employee => employee.AllowedScheduleEnd);
        builder.Property(employee => employee.AllowedZones).HasMaxLength(500);
        builder.Property(employee => employee.PerRideLimit).HasPrecision(10, 2);

        builder.Property(employee => employee.IsActive).IsRequired();
        builder.Property(employee => employee.CreatedAt).IsRequired();
    }
}
