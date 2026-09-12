using Microsoft.Extensions.Options;
using SmartTaxi.Application.Identity.Documents.Abstractions;
using SmartTaxi.Infrastructure.Identity.Options;

namespace SmartTaxi.Infrastructure.Identity.Services;

internal sealed class DocumentUploadPolicy : IDocumentUploadPolicy
{
    private readonly DocumentStorageOptions _options;

    public DocumentUploadPolicy(IOptions<DocumentStorageOptions> options)
    {
        _options = options.Value;
    }

    public long MaxFileSizeBytes => _options.MaxFileSizeBytes;

    public IReadOnlyCollection<string> AllowedMimeTypes => _options.AllowedMimeTypes;
}
