namespace SmartTaxi.Application.Identity.Referrals.Abstractions;

public interface IReferralCodeGenerator
{
    /// <summary>Generates a new short, human-shareable referral code (CSPRNG-backed).</summary>
    string Generate();
}
