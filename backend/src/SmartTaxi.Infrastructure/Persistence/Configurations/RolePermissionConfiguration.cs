using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Infrastructure.Persistence.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermissionRecord>
{
    public void Configure(EntityTypeBuilder<RolePermissionRecord> builder)
    {
        builder.ToTable("RolePermissions");

        builder.HasKey(rp => new { rp.Role, rp.PermissionCode });

        builder.Property(rp => rp.Role)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(rp => rp.PermissionCode)
            .HasMaxLength(100)
            .IsRequired();
    }
}
