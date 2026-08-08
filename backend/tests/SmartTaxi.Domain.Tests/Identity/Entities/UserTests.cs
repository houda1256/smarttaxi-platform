using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Domain.Tests.Identity.Entities;

public class UserTests
{
    private static User CreateUser(UserRole initialRole = UserRole.Customer)
    {
        var email = Email.Create("user@example.com");
        var passwordHash = HashedPassword.Create("hashed-value");
        return User.Create(email, passwordHash, initialRole);
    }

    [Fact]
    public void Create_AssignsProvidedValuesAndGeneratesId()
    {
        var email = Email.Create("user@example.com");
        var passwordHash = HashedPassword.Create("hashed-value");

        var user = User.Create(email, passwordHash, UserRole.Customer);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(email, user.Email);
        Assert.Equal(passwordHash, user.PasswordHash);
        Assert.True(user.HasRole(UserRole.Customer));
        Assert.Single(user.Roles);
        Assert.True(user.IsActive);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var user = CreateUser();

        user.Deactivate();

        Assert.False(user.IsActive);
    }

    [Fact]
    public void Activate_AfterDeactivate_SetsIsActiveTrue()
    {
        var user = CreateUser();
        user.Deactivate();

        user.Activate();

        Assert.True(user.IsActive);
    }

    [Fact]
    public void Create_TwiceWithSameEmail_ProducesDifferentIds()
    {
        var email = Email.Create("user@example.com");
        var passwordHash = HashedPassword.Create("hashed-value");

        var first = User.Create(email, passwordHash, UserRole.Customer);
        var second = User.Create(email, passwordHash, UserRole.Customer);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void AssignRole_WithNewRole_AddsIt()
    {
        var user = CreateUser();

        user.AssignRole(UserRole.Driver);

        Assert.True(user.HasRole(UserRole.Customer));
        Assert.True(user.HasRole(UserRole.Driver));
        Assert.Equal(2, user.Roles.Count);
    }

    [Fact]
    public void AssignRole_WithAlreadyHeldRole_IsIdempotent()
    {
        var user = CreateUser();

        user.AssignRole(UserRole.Customer);

        Assert.Single(user.Roles);
    }

    [Fact]
    public void RemoveRole_WhenMoreThanOneRoleHeld_RemovesIt()
    {
        var user = CreateUser();
        user.AssignRole(UserRole.Driver);

        user.RemoveRole(UserRole.Customer);

        Assert.False(user.HasRole(UserRole.Customer));
        Assert.True(user.HasRole(UserRole.Driver));
        Assert.Single(user.Roles);
    }

    [Fact]
    public void RemoveRole_WhenItWouldLeaveZeroRoles_Throws()
    {
        var user = CreateUser();

        Assert.Throws<InvalidOperationException>(() => user.RemoveRole(UserRole.Customer));
        Assert.True(user.HasRole(UserRole.Customer));
    }

    [Fact]
    public void RemoveRole_WithRoleNotHeld_IsNoOp()
    {
        var user = CreateUser();

        user.RemoveRole(UserRole.Driver);

        Assert.Single(user.Roles);
        Assert.True(user.HasRole(UserRole.Customer));
    }

    [Fact]
    public void CanRemoveRole_WhenItWouldLeaveZeroRoles_ReturnsFalse()
    {
        var user = CreateUser();

        Assert.False(user.CanRemoveRole(UserRole.Customer));
    }

    [Fact]
    public void CanRemoveRole_WhenMoreThanOneRoleHeld_ReturnsTrue()
    {
        var user = CreateUser();
        user.AssignRole(UserRole.Driver);

        Assert.True(user.CanRemoveRole(UserRole.Customer));
    }

    [Fact]
    public void CanRemoveRole_WithRoleNotHeld_ReturnsTrue()
    {
        var user = CreateUser();

        Assert.True(user.CanRemoveRole(UserRole.Driver));
    }

    [Fact]
    public void ChangePassword_ReplacesTheHash()
    {
        var user = CreateUser();
        var newHash = HashedPassword.Create("new-hashed-value");

        user.ChangePassword(newHash);

        Assert.Equal(newHash, user.PasswordHash);
    }

    [Fact]
    public void VerifyEmail_SetsEmailVerifiedAt()
    {
        var user = CreateUser();
        var utcNow = DateTime.UtcNow;

        user.VerifyEmail(utcNow);

        Assert.Equal(utcNow, user.EmailVerifiedAt);
    }

    [Fact]
    public void VerifyEmail_CalledTwice_KeepsFirstTimestamp()
    {
        var user = CreateUser();
        var firstAt = DateTime.UtcNow;

        user.VerifyEmail(firstAt);
        user.VerifyEmail(firstAt.AddMinutes(5));

        Assert.Equal(firstAt, user.EmailVerifiedAt);
    }

    [Fact]
    public void SetPhoneNumber_StoresItAndClearsAnyPriorVerification()
    {
        var user = CreateUser();
        user.SetPhoneNumber("+21612345678");
        user.VerifyPhone(DateTime.UtcNow);

        user.SetPhoneNumber("+14155552671");

        Assert.Equal("+14155552671", user.PhoneNumber);
        Assert.Null(user.PhoneVerifiedAt);
    }

    [Fact]
    public void VerifyPhone_SetsPhoneVerifiedAt()
    {
        var user = CreateUser();
        user.SetPhoneNumber("+21612345678");
        var utcNow = DateTime.UtcNow;

        user.VerifyPhone(utcNow);

        Assert.Equal(utcNow, user.PhoneVerifiedAt);
    }

    [Fact]
    public void BeginTwoFactorEnrollment_DoesNotEnableTwoFactor()
    {
        var user = CreateUser();

        user.BeginTwoFactorEnrollment("encrypted-secret", DateTime.UtcNow);

        Assert.False(user.TwoFactorEnabled);
        Assert.Null(user.TwoFactorActiveSecretEncrypted);
    }

    [Fact]
    public void ConfirmTwoFactorEnrollment_WithPendingSecret_EnablesTwoFactor()
    {
        var user = CreateUser();
        user.BeginTwoFactorEnrollment("encrypted-secret", DateTime.UtcNow);

        var confirmed = user.ConfirmTwoFactorEnrollment(DateTime.UtcNow);

        Assert.True(confirmed);
        Assert.True(user.TwoFactorEnabled);
        Assert.Equal("encrypted-secret", user.TwoFactorActiveSecretEncrypted);
        Assert.Null(user.TwoFactorPendingSecretEncrypted);
    }

    [Fact]
    public void ConfirmTwoFactorEnrollment_WithoutPendingSecret_ReturnsFalse()
    {
        var user = CreateUser();

        var confirmed = user.ConfirmTwoFactorEnrollment(DateTime.UtcNow);

        Assert.False(confirmed);
        Assert.False(user.TwoFactorEnabled);
    }

    [Fact]
    public void BeginTwoFactorEnrollment_WhileAlreadyEnabled_DoesNotDisruptActiveSecret()
    {
        var user = CreateUser();
        user.BeginTwoFactorEnrollment("original-secret", DateTime.UtcNow);
        user.ConfirmTwoFactorEnrollment(DateTime.UtcNow);

        user.BeginTwoFactorEnrollment("new-pending-secret", DateTime.UtcNow);

        Assert.True(user.TwoFactorEnabled);
        Assert.Equal("original-secret", user.TwoFactorActiveSecretEncrypted);
        Assert.Equal("new-pending-secret", user.TwoFactorPendingSecretEncrypted);
    }

    [Fact]
    public void DisableTwoFactor_ClearsAllTwoFactorState()
    {
        var user = CreateUser();
        user.BeginTwoFactorEnrollment("secret", DateTime.UtcNow);
        user.ConfirmTwoFactorEnrollment(DateTime.UtcNow);

        user.DisableTwoFactor();

        Assert.False(user.TwoFactorEnabled);
        Assert.Null(user.TwoFactorActiveSecretEncrypted);
        Assert.Null(user.TwoFactorConfirmedAt);
    }

    [Fact]
    public void EnsureReferralCode_WhenNoneAssignedYet_AssignsTheGivenCode()
    {
        var user = CreateUser();

        user.EnsureReferralCode("ABC123");

        Assert.Equal("ABC123", user.ReferralCode);
    }

    [Fact]
    public void EnsureReferralCode_WhenAlreadyAssigned_DoesNotOverwriteIt()
    {
        var user = CreateUser();
        user.EnsureReferralCode("FIRST01");

        user.EnsureReferralCode("SECOND02");

        Assert.Equal("FIRST01", user.ReferralCode);
    }

    [Fact]
    public void Anonymize_ScrubsPiiButKeepsTheEntityIntact()
    {
        var user = CreateUser();
        var originalId = user.Id;
        user.SetPhoneNumber("+15551234567");
        user.EnsureReferralCode("REF001");
        user.BeginTwoFactorEnrollment("secret", DateTime.UtcNow);
        user.ConfirmTwoFactorEnrollment(DateTime.UtcNow);

        var anonymizedEmail = Email.Create($"deleted-{Guid.NewGuid()}@anonymized.smarttaxi.invalid");
        var unusableHash = HashedPassword.Create("unusable-hash");
        user.Anonymize(anonymizedEmail, unusableHash);

        Assert.Equal(originalId, user.Id);
        Assert.Equal(anonymizedEmail, user.Email);
        Assert.Equal(unusableHash, user.PasswordHash);
        Assert.Null(user.PhoneNumber);
        Assert.Null(user.PhoneVerifiedAt);
        Assert.False(user.TwoFactorEnabled);
        Assert.Null(user.TwoFactorActiveSecretEncrypted);
        Assert.Null(user.ReferralCode);
        // Roles/eligibility history are untouched — anonymization scrubs PII, not the account's history.
        Assert.True(user.HasRole(UserRole.Customer));
    }
}
