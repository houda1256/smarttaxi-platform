using System.Text;
using System.Text.Json;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.Identity.Documents.Abstractions;
using SmartTaxi.Application.Identity.Preferences.Abstractions;
using SmartTaxi.Application.Identity.Referrals.Abstractions;
using SmartTaxi.Domain.Identity.Entities;

namespace SmartTaxi.Application.Identity.DataRequests;

/// <summary>
/// Builds a JSON snapshot of everything this module knows about a user, for
/// GDPR-style export requests. Contains document *metadata* only — never the
/// underlying binary content — to keep the export self-contained and simple.
/// </summary>
public sealed class PersonalDataExportBuilder
{
    private readonly IUserRepository _userRepository;
    private readonly IUserDocumentRepository _documentRepository;
    private readonly IUserPreferencesRepository _preferencesRepository;
    private readonly IReferralRepository _referralRepository;
    private readonly IFileStorageService _fileStorage;

    public PersonalDataExportBuilder(
        IUserRepository userRepository,
        IUserDocumentRepository documentRepository,
        IUserPreferencesRepository preferencesRepository,
        IReferralRepository referralRepository,
        IFileStorageService fileStorage)
    {
        _userRepository = userRepository;
        _documentRepository = documentRepository;
        _preferencesRepository = preferencesRepository;
        _referralRepository = referralRepository;
        _fileStorage = fileStorage;
    }

    public async Task<string> BuildAndStoreAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        var documents = await _documentRepository.GetForUserAsync(userId, cancellationToken);
        var preferences = await _preferencesRepository.GetByUserIdAsync(userId, cancellationToken);
        var referralsMade = await _referralRepository.GetForReferrerAsync(userId, cancellationToken);
        var sponsoredBy = await _referralRepository.GetByRefereeUserIdAsync(userId, cancellationToken);

        var export = new
        {
            Profile = user is null
                ? null
                : new
                {
                    user.Id,
                    Email = user.Email.Value,
                    Roles = user.Roles.Select(r => r.ToString()).ToList(),
                    user.IsActive,
                    user.EmailVerifiedAt,
                    user.PhoneVerifiedAt,
                    user.ReferralCode
                },
            Documents = documents.Select(d => new
            {
                d.Id,
                DocumentType = d.DocumentType.ToString(),
                Status = d.Status.ToString(),
                d.FileName,
                d.CreatedAt
            }),
            Preferences = preferences is null
                ? null
                : new
                {
                    Language = preferences.Language.ToString(),
                    NotificationChannels = preferences.NotificationChannels.ToString(),
                    preferences.Timezone,
                    preferences.ShareProfileWithPartners,
                    preferences.AllowMarketingCommunications
                },
            ReferralsMade = referralsMade.Select(r => new { r.Id, r.RefereeUserId, Status = r.Status.ToString(), r.CreatedAt }),
            SponsoredBy = sponsoredBy is null ? null : new { sponsoredBy.ReferrerUserId, Status = sponsoredBy.Status.ToString() }
        };

        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true });
        var storageKey = $"exports/{userId}/{Guid.NewGuid()}.json";

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        await _fileStorage.SaveAsync(storageKey, stream, cancellationToken);

        return storageKey;
    }
}
