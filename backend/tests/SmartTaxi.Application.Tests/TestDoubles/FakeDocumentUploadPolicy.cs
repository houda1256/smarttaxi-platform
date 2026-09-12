using SmartTaxi.Application.Identity.Documents.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeDocumentUploadPolicy : IDocumentUploadPolicy
{
    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024;

    public IReadOnlyCollection<string> AllowedMimeTypes { get; init; } = ["application/pdf", "image/jpeg", "image/png"];
}
