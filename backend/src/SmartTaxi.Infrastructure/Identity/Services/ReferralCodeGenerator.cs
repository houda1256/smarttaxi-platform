using System.Security.Cryptography;
using System.Text;
using SmartTaxi.Application.Identity.Referrals.Abstractions;

namespace SmartTaxi.Infrastructure.Identity.Services;

/// <summary>
/// CSPRNG-backed short code, excluding visually ambiguous characters
/// (0/O, 1/I/L) so codes are easy for a human to read back and share.
/// </summary>
internal sealed class ReferralCodeGenerator : IReferralCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int Length = 10;

    public string Generate()
    {
        var builder = new StringBuilder(Length);

        for (var i = 0; i < Length; i++)
        {
            builder.Append(Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)]);
        }

        return builder.ToString();
    }
}
