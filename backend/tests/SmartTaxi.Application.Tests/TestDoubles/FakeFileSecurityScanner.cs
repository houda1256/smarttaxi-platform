using SmartTaxi.Application.Advertising.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeFileSecurityScanner : IFileSecurityScanner
{
    public bool NextScanIsSafe { get; set; } = true;

    public Task<FileScanResult> ScanAsync(byte[] content, string mimeType, CancellationToken cancellationToken) =>
        Task.FromResult(NextScanIsSafe ? new FileScanResult(true, null) : new FileScanResult(false, "Échec du contrôle de sécurité (test)."));
}
