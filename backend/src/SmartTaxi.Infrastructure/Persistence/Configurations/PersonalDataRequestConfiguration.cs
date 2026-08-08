using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Identity.DataRequests.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class PersonalDataRequestConfiguration : IEntityTypeConfiguration<PersonalDataRequest>
{
    public void Configure(EntityTypeBuilder<PersonalDataRequest> builder)
    {
        builder.ToTable("PersonalDataRequests");

        builder.HasKey(request => request.Id);
        builder.Property(request => request.Id).ValueGeneratedNever();

        builder.Property(request => request.UserId).IsRequired();
        builder.HasIndex(request => request.UserId);

        builder.Property(request => request.RequestType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(request => request.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasIndex(request => request.Status);

        builder.Property(request => request.ProcessingNotes).HasMaxLength(1000);
        builder.Property(request => request.ResultReference).HasMaxLength(500);

        builder.Property(request => request.RequestedAt).IsRequired();
        builder.Property(request => request.CreatedAt).IsRequired();
        builder.Property(request => request.UpdatedAt).IsRequired();
    }
}
