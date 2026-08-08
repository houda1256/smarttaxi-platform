namespace SmartTaxi.Application.Identity.Documents.Abstractions;

public interface IDocumentUploadPolicy
{
    long MaxFileSizeBytes { get; }

    IReadOnlyCollection<string> AllowedMimeTypes { get; }
}
