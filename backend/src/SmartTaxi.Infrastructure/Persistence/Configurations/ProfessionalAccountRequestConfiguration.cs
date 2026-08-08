using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Identity.Professional.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class ProfessionalAccountRequestConfiguration : IEntityTypeConfiguration<ProfessionalAccountRequest>
{
    public void Configure(EntityTypeBuilder<ProfessionalAccountRequest> builder)
    {
        builder.ToTable("ProfessionalAccountRequests");

        builder.HasKey(request => request.Id);
        builder.Property(request => request.Id).ValueGeneratedNever();

        builder.Property(request => request.UserId).IsRequired();
        builder.HasIndex(request => request.UserId);

        builder.Property(request => request.Role).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(request => request.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasIndex(request => request.Status);

        builder.Property(request => request.RejectionReason).HasMaxLength(1000);
        builder.Property(request => request.ReviewComment).HasMaxLength(1000);

        builder.Property(request => request.CreatedAt).IsRequired();
        builder.Property(request => request.UpdatedAt).IsRequired();
    }
}
