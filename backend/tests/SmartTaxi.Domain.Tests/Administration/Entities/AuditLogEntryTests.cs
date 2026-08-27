using SmartTaxi.Domain.Administration.Entities;
using SmartTaxi.Domain.Administration.Enums;

namespace SmartTaxi.Domain.Tests.Administration.Entities;

public class AuditLogEntryTests
{
    [Fact]
    public void Create_ValidRequest_Succeeds()
    {
        var entry = AuditLogEntry.Create(
            Guid.NewGuid(), AuditAction.AdminUserSuspended, AuditTargetType.User, Guid.NewGuid(), null, null, null, null, DateTime.UtcNow);

        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Null(entry.Metadata);
    }

    [Fact]
    public void Create_SystemTriggeredEvent_AllowsNullActor()
    {
        var entry = AuditLogEntry.Create(
            null, AuditAction.AccountLocked, AuditTargetType.User, Guid.NewGuid(), null, null, null, null, DateTime.UtcNow);

        Assert.Null(entry.ActorUserId);
    }

    [Fact]
    public void Create_EmptyTargetId_Throws()
    {
        Assert.Throws<ArgumentException>(() => AuditLogEntry.Create(
            Guid.NewGuid(), AuditAction.RoleAssigned, AuditTargetType.User, Guid.Empty, null, null, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithAllowedMetadata_SerializesExactly()
    {
        var entry = AuditLogEntry.Create(
            Guid.NewGuid(), AuditAction.RoleAssigned, AuditTargetType.User, Guid.NewGuid(),
            new Dictionary<string, string> { ["role"] = "Driver" }, null, null, null, DateTime.UtcNow);

        Assert.Contains("\"role\":\"Driver\"", entry.Metadata);
    }

    [Fact]
    public void Create_OversizedCorrelationId_IsTruncatedNotRejected()
    {
        var oversized = new string('a', 200);

        var entry = AuditLogEntry.Create(
            Guid.NewGuid(), AuditAction.RoleAssigned, AuditTargetType.User, Guid.NewGuid(), null, oversized, null, null, DateTime.UtcNow);

        Assert.Equal(64, entry.CorrelationId!.Length);
    }

    [Fact]
    public void Create_NullContextFields_ProducesValidEntryForNonHttpCallers()
    {
        var entry = AuditLogEntry.Create(
            null, AuditAction.AccountLocked, AuditTargetType.User, Guid.NewGuid(), null, null, null, null, DateTime.UtcNow);

        Assert.Null(entry.CorrelationId);
        Assert.Null(entry.IpAddress);
        Assert.Null(entry.UserAgent);
    }
}
