namespace SmartTaxi.Application.Advertising.Abstractions;

/// <summary>
/// Content/security scanning abstraction for uploaded creatives — the
/// Advertising specification expects this but the repository had no such
/// abstraction before Module 8. No real external content-moderation/antivirus
/// provider is integrated; the dev implementation performs deterministic,
/// safe, non-external validation only (see DevFileSecurityScanner). A failed
/// scan must prevent the creative from ever becoming reviewable/usable.
/// </summary>
public interface IFileSecurityScanner
{
    Task<FileScanResult> ScanAsync(byte[] content, string mimeType, CancellationToken cancellationToken);
}

public sealed record FileScanResult(bool IsSafe, string? RejectionReason);
