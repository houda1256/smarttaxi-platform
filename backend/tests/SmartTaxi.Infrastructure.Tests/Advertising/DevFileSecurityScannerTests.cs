using SmartTaxi.Infrastructure.Advertising.Services;

namespace SmartTaxi.Infrastructure.Tests.Advertising;

public class DevFileSecurityScannerTests
{
    private readonly DevFileSecurityScanner _scanner = new();

    [Fact]
    public async Task ScanAsync_NormalImageBytes_IsSafe()
    {
        var result = await _scanner.ScanAsync([0x89, 0x50, 0x4E, 0x47, 1, 2, 3], "image/png", CancellationToken.None);

        Assert.True(result.IsSafe);
        Assert.Null(result.RejectionReason);
    }

    [Fact]
    public async Task ScanAsync_EmptyContent_IsRejected()
    {
        var result = await _scanner.ScanAsync([], "image/png", CancellationToken.None);

        Assert.False(result.IsSafe);
        Assert.NotNull(result.RejectionReason);
    }

    [Fact]
    public async Task ScanAsync_WindowsExecutableSignature_IsRejected()
    {
        var result = await _scanner.ScanAsync([0x4D, 0x5A, 1, 2, 3], "image/png", CancellationToken.None);

        Assert.False(result.IsSafe);
    }

    [Fact]
    public async Task ScanAsync_ShellScriptSignature_IsRejected()
    {
        var result = await _scanner.ScanAsync([0x23, 0x21, 1, 2, 3], "video/mp4", CancellationToken.None);

        Assert.False(result.IsSafe);
    }
}
