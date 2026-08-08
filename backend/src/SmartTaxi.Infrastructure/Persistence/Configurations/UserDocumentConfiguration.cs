using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Identity.Documents.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class UserDocumentConfiguration : IEntityTypeConfiguration<UserDocument>
{
    public void Configure(EntityTypeBuilder<UserDocument> builder)
    {
        builder.ToTable("UserDocuments");

        builder.HasKey(document => document.Id);
        builder.Property(document => document.Id).ValueGeneratedNever();

        builder.Property(document => document.UserId).IsRequired();
        builder.HasIndex(document => document.UserId);

        builder.Property(document => document.DocumentType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(document => document.Status).HasConversion<string>().HasMaxLength(50).IsRequired();

        builder.Property(document => document.FileReference).HasMaxLength(500).IsRequired();
        builder.Property(document => document.FileName).HasMaxLength(255).IsRequired();
        builder.Property(document => document.MimeType).HasMaxLength(100).IsRequired();
        builder.Property(document => document.Sha256).HasMaxLength(64).IsRequired();

        builder.HasIndex(document => new { document.UserId, document.DocumentType, document.Sha256 });

        builder.Property(document => document.RejectionReason).HasMaxLength(1000);
        builder.Property(document => document.ReviewComment).HasMaxLength(1000);

        builder.Property(document => document.CreatedAt).IsRequired();
        builder.Property(document => document.UpdatedAt).IsRequired();

        builder.HasIndex(document => document.Status);
    }
}
