using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.RoadsideAssistance.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class RoadsidePartnerSelectionHistoryConfiguration : IEntityTypeConfiguration<RoadsidePartnerSelectionHistory>
{
    public void Configure(EntityTypeBuilder<RoadsidePartnerSelectionHistory> builder)
    {
        builder.ToTable("RoadsidePartnerSelectionHistories");

        builder.HasKey(history => history.Id);
        builder.Property(history => history.Id).ValueGeneratedNever();

        builder.Property(history => history.RoadsideAssistanceRequestId).IsRequired();
        builder.HasIndex(history => history.RoadsideAssistanceRequestId);

        builder.Property(history => history.CycleNumber).IsRequired();
        builder.HasIndex(history => new { history.RoadsideAssistanceRequestId, history.CycleNumber }).IsUnique();

        builder.Property(history => history.SelectedPartnerUserId).IsRequired();
        builder.Property(history => history.SelectedAtUtc).IsRequired();

        builder.Property(history => history.Response).HasConversion<string>().HasMaxLength(20);
        builder.Property(history => history.RejectionReason).HasMaxLength(1000);
        builder.Property(history => history.RespondedAtUtc);
    }
}
