using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Domain.Tests.Advertising.Entities;

public class AdvertisingPlacementTests
{
    [Fact]
    public void Create_WithValidFields_IsActiveByDefault()
    {
        var placement = AdvertisingPlacement.Create("SCREEN_HOME", "Home Screen", "desc", [AdMediaType.Image, AdMediaType.Video], DateTime.UtcNow);

        Assert.True(placement.IsActive);
        Assert.Equal("SCREEN_HOME", placement.Code);
        Assert.True(placement.Supports(AdMediaType.Image));
        Assert.False(placement.Supports(AdMediaType.Banner));
    }

    [Fact]
    public void Create_NoSupportedMediaTypes_Throws()
    {
        Assert.Throws<ArgumentException>(() => AdvertisingPlacement.Create("CODE", "Name", "desc", [], DateTime.UtcNow));
    }

    [Fact]
    public void Deactivate_ThenActivate_TogglesIsActive()
    {
        var placement = AdvertisingPlacement.Create("CODE", "Name", "desc", [AdMediaType.Banner], DateTime.UtcNow);

        placement.Deactivate(DateTime.UtcNow);
        Assert.False(placement.IsActive);

        placement.Activate(DateTime.UtcNow);
        Assert.True(placement.IsActive);
    }
}
