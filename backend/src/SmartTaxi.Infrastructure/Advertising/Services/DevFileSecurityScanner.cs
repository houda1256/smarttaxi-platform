using SmartTaxi.Application.Advertising.Abstractions;

namespace SmartTaxi.Infrastructure.Advertising.Services;

/// <summary>
/// Dev stub — no real antivirus/content-moderation provider is integrated
/// (explicitly out of scope, same convention as the mock IPaymentGateway/
/// IInvoicePdfGenerator elsewhere in this codebase). Performs deterministic,
/// non-external validation only: rejects empty content and a small set of
/// executable/script byte signatures that should never appear in an
/// image/video creative. This is NOT a substitute for a real scanner in
/// production — it exists solely so the IFileSecurityScanner seam is
/// exercised end-to-end.
/// </summary>
internal sealed class DevFileSecurityScanner : IFileSecurityScanner
{
    private const string EmptyContentRejection = "Le fichier est vide.";
    private const string ExecutableSignatureRejection = "Le fichier a été rejeté par le contrôle de sécurité (signature suspecte).";

    // "MZ" (Windows PE executables) and "#!" (shell scripts) should never appear at the start of an image/video creative.
    private static readonly byte[][] BannedLeadingSignatures = [[0x4D, 0x5A], [0x23, 0x21]];

    public Task<FileScanResult> ScanAsync(byte[] content, string mimeType, CancellationToken cancellationToken)
    {
        if (content.Length == 0)
        {
            return Task.FromResult(new FileScanResult(false, EmptyContentRejection));
        }

        foreach (var signature in BannedLeadingSignatures)
        {
            if (content.Length >= signature.Length && content.AsSpan(0, signature.Length).SequenceEqual(signature))
            {
                return Task.FromResult(new FileScanResult(false, ExecutableSignatureRejection));
            }
        }

        return Task.FromResult(new FileScanResult(true, null));
    }
}
