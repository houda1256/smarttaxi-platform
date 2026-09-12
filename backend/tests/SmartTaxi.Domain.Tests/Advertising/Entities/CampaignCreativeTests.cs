using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Domain.Tests.Advertising.Entities;

public class CampaignCreativeTests
{
    [Fact]
    public void Upload_WithValidFields_StartsPendingReviewAtVersion1()
    {
        var creative = CampaignCreative.Upload(Guid.NewGuid(), AdMediaType.Image, "image/png", "banner.png", "storage-key-1", 1024, "abc123", DateTime.UtcNow);

        Assert.Equal(AdMediaStatus.PendingReview, creative.Status);
        Assert.Equal(1, creative.Version);
        Assert.Null(creative.ReplacesCreativeId);
    }

    [Fact]
    public void CreateReplacement_BuildsNextVersionReferencingPrevious()
    {
        var original = CampaignCreative.Upload(Guid.NewGuid(), AdMediaType.Image, "image/png", "banner.png", "storage-key-1", 1024, "abc123", DateTime.UtcNow);

        var replacement = original.CreateReplacement(AdMediaType.Image, "image/png", "banner-v2.png", "storage-key-2", 2048, "def456", DateTime.UtcNow);

        Assert.Equal(2, replacement.Version);
        Assert.Equal(original.Id, replacement.ReplacesCreativeId);
        Assert.Equal(AdMediaStatus.PendingReview, replacement.Status);
    }

    [Theory]
    [InlineData("", "name.png", "key", 100, "hash")]
    [InlineData("image/png", "", "key", 100, "hash")]
    [InlineData("image/png", "name.png", "", 100, "hash")]
    [InlineData("image/png", "name.png", "key", 0, "hash")]
    [InlineData("image/png", "name.png", "key", 100, "")]
    public void Upload_InvalidField_Throws(string mimeType, string displayName, string storageKey, long size, string hash)
    {
        Assert.Throws<ArgumentException>(() =>
            CampaignCreative.Upload(Guid.NewGuid(), AdMediaType.Image, mimeType, displayName, storageKey, size, hash, DateTime.UtcNow));
    }
}
