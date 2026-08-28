using SmartTaxi.Application.Administration.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeAuditContextAccessor : IAuditContextAccessor
{
    public AuditContextInfo Context { get; set; } = AuditContextInfo.None;

    public AuditContextInfo GetContext() => Context;
}
