using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Domain.Identity.Entities;

public sealed class User : AggregateRoot
{
    private readonly List<UserRoleAssignment> _roleAssignments = [];

    public Email Email { get; private set; }
    public HashedPassword PasswordHash { get; private set; }
    public bool IsActive { get; private set; } = true;

    public string? PhoneNumber { get; private set; }
    public DateTime? EmailVerifiedAt { get; private set; }
    public DateTime? PhoneVerifiedAt { get; private set; }

    public bool TwoFactorEnabled { get; private set; }
    public string? TwoFactorActiveSecretEncrypted { get; private set; }
    public string? TwoFactorPendingSecretEncrypted { get; private set; }
    public DateTime? TwoFactorPendingSecretCreatedAt { get; private set; }
    public DateTime? TwoFactorConfirmedAt { get; private set; }

    public string? ReferralCode { get; private set; }

    public IReadOnlyCollection<UserRole> Roles =>
        _roleAssignments.Select(assignment => assignment.Role).ToList();

    private User(Guid id, Email email, HashedPassword passwordHash)
        : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
    }

    public static User Create(Email email, HashedPassword passwordHash, UserRole initialRole)
    {
        var user = new User(Guid.NewGuid(), email, passwordHash);
        user.AssignRole(initialRole);
        return user;
    }

    public bool HasRole(UserRole role) => _roleAssignments.Any(assignment => assignment.Role == role);

    public void AssignRole(UserRole role)
    {
        if (HasRole(role))
        {
            return;
        }

        _roleAssignments.Add(new UserRoleAssignment(Id, role));
    }

    public bool CanRemoveRole(UserRole role) => !HasRole(role) || _roleAssignments.Count > 1;

    public void RemoveRole(UserRole role)
    {
        if (!HasRole(role))
        {
            return;
        }

        if (!CanRemoveRole(role))
        {
            throw new InvalidOperationException("Un utilisateur doit toujours conserver au moins un rôle.");
        }

        _roleAssignments.RemoveAll(assignment => assignment.Role == role);
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public void ChangePassword(HashedPassword newPasswordHash)
    {
        PasswordHash = newPasswordHash;
    }

    public void VerifyEmail(DateTime utcNow)
    {
        EmailVerifiedAt ??= utcNow;
    }

    /// <summary>
    /// Attaches (or replaces) the user's phone number. A replaced number always
    /// requires re-verification, so any prior verification is cleared.
    /// </summary>
    public void SetPhoneNumber(string phoneNumber)
    {
        PhoneNumber = phoneNumber;
        PhoneVerifiedAt = null;
    }

    public void VerifyPhone(DateTime utcNow)
    {
        PhoneVerifiedAt ??= utcNow;
    }

    /// <summary>
    /// Stores a new, not-yet-active TOTP secret. Deliberately kept separate from
    /// TwoFactorActiveSecretEncrypted so that starting a new enrollment never
    /// disrupts an already-confirmed, currently-active 2FA setup until the new
    /// enrollment is itself confirmed.
    /// </summary>
    public void BeginTwoFactorEnrollment(string pendingSecretEncrypted, DateTime utcNow)
    {
        TwoFactorPendingSecretEncrypted = pendingSecretEncrypted;
        TwoFactorPendingSecretCreatedAt = utcNow;
    }

    /// <summary>
    /// Activates 2FA using the pending secret. Returns false if there is no
    /// pending enrollment to confirm.
    /// </summary>
    public bool ConfirmTwoFactorEnrollment(DateTime utcNow)
    {
        if (TwoFactorPendingSecretEncrypted is null)
        {
            return false;
        }

        TwoFactorActiveSecretEncrypted = TwoFactorPendingSecretEncrypted;
        TwoFactorEnabled = true;
        TwoFactorConfirmedAt = utcNow;
        TwoFactorPendingSecretEncrypted = null;
        TwoFactorPendingSecretCreatedAt = null;
        return true;
    }

    public void DisableTwoFactor()
    {
        TwoFactorEnabled = false;
        TwoFactorActiveSecretEncrypted = null;
        TwoFactorConfirmedAt = null;
        TwoFactorPendingSecretEncrypted = null;
        TwoFactorPendingSecretCreatedAt = null;
    }

    /// <summary>Idempotent — only assigns a code if this user doesn't already have one.</summary>
    public void EnsureReferralCode(string generatedCode)
    {
        ReferralCode ??= generatedCode;
    }

    /// <summary>
    /// Scrubs personally-identifiable fields for a GDPR-style deletion/anonymization
    /// request. Deliberately never removes the row itself — UserDocuments and audit
    /// entries reference this user's Id, and destroying it would break that history.
    /// </summary>
    public void Anonymize(Email anonymizedEmail, HashedPassword unusablePasswordHash)
    {
        Email = anonymizedEmail;
        PasswordHash = unusablePasswordHash;
        PhoneNumber = null;
        PhoneVerifiedAt = null;
        TwoFactorEnabled = false;
        TwoFactorActiveSecretEncrypted = null;
        TwoFactorPendingSecretEncrypted = null;
        TwoFactorPendingSecretCreatedAt = null;
        TwoFactorConfirmedAt = null;
        ReferralCode = null;
    }
}
