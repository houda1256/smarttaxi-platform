using SmartTaxi.Application.Advertising.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeAdvertisingMediaUploadPolicy : IAdvertisingMediaUploadPolicy
{
    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024;

    public IReadOnlyCollection<string> AllowedMimeTypes { get; set; } = ["image/jpeg", "image/png", "video/mp4"];
}
