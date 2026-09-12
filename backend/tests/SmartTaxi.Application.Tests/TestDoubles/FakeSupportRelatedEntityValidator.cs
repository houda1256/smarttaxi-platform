using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

/// <summary>Simple toggle for tests not focused on related-entity validation itself — see SupportRelatedEntityValidatorTests for tests of the real composition logic against the existing per-module fakes.</summary>
public sealed class FakeSupportRelatedEntityValidator : ISupportRelatedEntityValidator
{
    public bool AlwaysValid { get; set; } = true;

    public Task<bool> IsValidReferenceAsync(SupportRelatedEntityType type, Guid entityId, Guid callerUserId, CancellationToken cancellationToken) =>
        Task.FromResult(AlwaysValid);
}
