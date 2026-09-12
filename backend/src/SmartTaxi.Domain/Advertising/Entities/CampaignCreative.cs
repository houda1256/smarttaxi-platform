using SmartTaxi.Domain.Advertising.Enums;
using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Advertising.Entities;

/// <summary>
/// One row per uploaded creative version — replacing a creative never mutates
/// the previous row's bytes/metadata (same "CreateReplacement, never edit in
/// place" convention as UserDocument): a new row is inserted at Version+1 and
/// the previous row is atomically marked Replaced by the repository. Approval
/// status transitions are repository-level atomic guards, same convention as
/// AdCampaign's own lifecycle.
/// </summary>
public sealed class CampaignCreative : Entity
{
    public Guid CampaignId { get; private set; }
    public AdMediaType MediaType { get; private set; }
    public string MimeType { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string StorageKey { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public string Sha256 { get; private set; } = string.Empty;
    public AdMediaStatus Status { get; private set; }
    public int Version { get; private set; }
    public Guid? ReplacesCreativeId { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private CampaignCreative()
    {
    }

    private CampaignCreative(
        Guid campaignId, AdMediaType mediaType, string mimeType, string displayName, string storageKey, long fileSizeBytes,
        string sha256, int version, Guid? replacesCreativeId, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        CampaignId = campaignId;
        MediaType = mediaType;
        MimeType = mimeType;
        DisplayName = displayName;
        StorageKey = storageKey;
        FileSizeBytes = fileSizeBytes;
        Sha256 = sha256;
        Status = AdMediaStatus.PendingReview;
        Version = version;
        ReplacesCreativeId = replacesCreativeId;
        CreatedAtUtc = utcNow;
    }

    public static CampaignCreative Upload(
        Guid campaignId, AdMediaType mediaType, string mimeType, string displayName, string storageKey, long fileSizeBytes,
        string sha256, DateTime utcNow)
    {
        Validate(mimeType, displayName, storageKey, fileSizeBytes, sha256);
        return new CampaignCreative(campaignId, mediaType, mimeType, displayName.Trim(), storageKey, fileSizeBytes, sha256, 1, null, utcNow);
    }

    /// <summary>Builds the next version in this creative's chain — does not mutate this instance; the repository atomically marks this row Replaced in the same operation that inserts the new row.</summary>
    public CampaignCreative CreateReplacement(
        AdMediaType mediaType, string mimeType, string displayName, string storageKey, long fileSizeBytes, string sha256, DateTime utcNow)
    {
        Validate(mimeType, displayName, storageKey, fileSizeBytes, sha256);
        return new CampaignCreative(CampaignId, mediaType, mimeType, displayName.Trim(), storageKey, fileSizeBytes, sha256, Version + 1, Id, utcNow);
    }

    private static void Validate(string mimeType, string displayName, string storageKey, long fileSizeBytes, string sha256)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            throw new ArgumentException("Le type MIME est requis.");
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Le nom du fichier est requis.");
        }

        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("La clé de stockage est requise.");
        }

        if (fileSizeBytes <= 0)
        {
            throw new ArgumentException("La taille du fichier est invalide.");
        }

        if (string.IsNullOrWhiteSpace(sha256))
        {
            throw new ArgumentException("L'empreinte du fichier est requise.");
        }
    }
}
