using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class RideComplaintConfiguration : IEntityTypeConfiguration<RideComplaint>
{
    public void Configure(EntityTypeBuilder<RideComplaint> builder)
    {
        builder.ToTable("RideComplaints");

        builder.HasKey(complaint => complaint.Id);
        builder.Property(complaint => complaint.Id).ValueGeneratedNever();

        builder.Property(complaint => complaint.RideId).IsRequired();
        builder.HasIndex(complaint => complaint.RideId);

        builder.Property(complaint => complaint.ComplainantUserId).IsRequired();
        builder.Property(complaint => complaint.ConcernedUserId).IsRequired();
        builder.Property(complaint => complaint.Category).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(complaint => complaint.Description).HasMaxLength(2000).IsRequired();
        builder.Property(complaint => complaint.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(complaint => complaint.Status);
        builder.Property(complaint => complaint.Resolution).HasMaxLength(2000);
        builder.Property(complaint => complaint.CreatedAt).IsRequired();
        builder.Property(complaint => complaint.ResolvedAt);
    }
}
