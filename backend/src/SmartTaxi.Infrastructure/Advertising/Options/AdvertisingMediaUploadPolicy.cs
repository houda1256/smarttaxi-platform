using Microsoft.Extensions.Options;
using SmartTaxi.Application.Advertising.Abstractions;

namespace SmartTaxi.Infrastructure.Advertising.Options;

internal sealed class AdvertisingMediaUploadPolicy : IAdvertisingMediaUploadPolicy
{
    private readonly AdvertisingOptions _options;

    public AdvertisingMediaUploadPolicy(IOptions<AdvertisingOptions> options)
    {
        _options = options.Value;
    }

    public long MaxFileSizeBytes => _options.MaxMediaFileSizeBytes;

    public IReadOnlyCollection<string> AllowedMimeTypes => _options.AllowedMediaMimeTypes;
}
