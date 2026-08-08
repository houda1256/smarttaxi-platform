using System.Security.Cryptography;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Documents.Abstractions;

namespace SmartTaxi.Application.Identity.Documents;

/// <summary>
/// Shared MIME/size/hash validation for both initial upload and replacement —
/// the size limit is enforced against the actual bytes read, never a
/// client-supplied Content-Length header, and the hash is always computed
/// server-side.
/// </summary>
public sealed class DocumentUploadValidator
{
    private const string UnsupportedMimeTypeError = "Ce type de fichier n'est pas autorisé.";
    private const string OversizedFileError = "Le fichier dépasse la taille maximale autorisée.";

    private readonly IDocumentUploadPolicy _policy;

    public DocumentUploadValidator(IDocumentUploadPolicy policy)
    {
        _policy = policy;
    }

    public async Task<Result<ValidatedDocumentContent>> ValidateAndReadAsync(
        Stream content, string declaredMimeType, CancellationToken cancellationToken)
    {
        if (!_policy.AllowedMimeTypes.Contains(declaredMimeType))
        {
            return Result<ValidatedDocumentContent>.Failure(UnsupportedMimeTypeError, ErrorType.Validation);
        }

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        if (buffer.Length > _policy.MaxFileSizeBytes)
        {
            return Result<ValidatedDocumentContent>.Failure(OversizedFileError, ErrorType.Validation);
        }

        var bytes = buffer.ToArray();
        var sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes));

        return Result<ValidatedDocumentContent>.Success(new ValidatedDocumentContent(bytes, sha256));
    }
}

public sealed record ValidatedDocumentContent(byte[] Bytes, string Sha256);
