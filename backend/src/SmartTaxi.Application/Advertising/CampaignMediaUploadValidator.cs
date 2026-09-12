using System.Security.Cryptography;
using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;

namespace SmartTaxi.Application.Advertising;

/// <summary>
/// Shared MIME/size/hash validation for creative upload and replacement — the
/// size limit is enforced against the actual bytes read, never a
/// client-supplied Content-Length header, and the hash is always computed
/// server-side (same convention as DocumentUploadValidator). This only
/// validates format/size; IFileSecurityScanner is a separate, subsequent
/// check the command handler runs afterward — a failed scan must still block
/// the upload even if format/size passed.
/// </summary>
public sealed class CampaignMediaUploadValidator
{
    private const string UnsupportedMimeTypeError = "Ce type de fichier n'est pas autorisé pour un média publicitaire.";
    private const string OversizedFileError = "Le fichier dépasse la taille maximale autorisée.";

    private readonly IAdvertisingMediaUploadPolicy _policy;

    public CampaignMediaUploadValidator(IAdvertisingMediaUploadPolicy policy)
    {
        _policy = policy;
    }

    public async Task<Result<ValidatedMediaContent>> ValidateAndReadAsync(Stream content, string declaredMimeType, CancellationToken cancellationToken)
    {
        if (!_policy.AllowedMimeTypes.Contains(declaredMimeType))
        {
            return Result<ValidatedMediaContent>.Failure(UnsupportedMimeTypeError, ErrorType.Validation);
        }

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        if (buffer.Length > _policy.MaxFileSizeBytes)
        {
            return Result<ValidatedMediaContent>.Failure(OversizedFileError, ErrorType.Validation);
        }

        var bytes = buffer.ToArray();
        var sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes));

        return Result<ValidatedMediaContent>.Success(new ValidatedMediaContent(bytes, sha256));
    }
}

public sealed record ValidatedMediaContent(byte[] Bytes, string Sha256);
