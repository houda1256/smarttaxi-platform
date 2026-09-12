using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Infrastructure.Identity.Options;

namespace SmartTaxi.Infrastructure.Identity.Services;

// RFC 6238 (TOTP) / RFC 4226 (HOTP) — a real, complete implementation, not a
// mock: TOTP requires no external provider at all, the authenticator app is
// the user's own device computing the same algorithm from the shared secret.
internal sealed class TotpService : ITotpService
{
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    private const int SecretSizeBytes = 20; // 160 bits, the standard TOTP secret size

    private readonly TwoFactorOptions _options;

    public TotpService(IOptions<TwoFactorOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateSecret() => Base32Encode(RandomNumberGenerator.GetBytes(SecretSizeBytes));

    public string BuildAuthenticatorUri(string secret, string accountEmail, string issuer)
    {
        var label = Uri.EscapeDataString($"{issuer}:{accountEmail}");
        var encodedIssuer = Uri.EscapeDataString(issuer);

        return $"otpauth://totp/{label}?secret={secret}&issuer={encodedIssuer}" +
               $"&digits={_options.TotpDigits}&period={_options.TotpStepSeconds}";
    }

    public bool ValidateCode(string secret, string code, DateTime utcNow)
    {
        var secretBytes = Base32Decode(secret);
        var currentStep = GetStep(utcNow);
        var submitted = Encoding.ASCII.GetBytes(code.Trim().PadLeft(_options.TotpDigits, '0'));

        for (var drift = -_options.TotpDriftSteps; drift <= _options.TotpDriftSteps; drift++)
        {
            var expected = Encoding.ASCII.GetBytes(ComputeCode(secretBytes, currentStep + drift));

            if (CryptographicOperations.FixedTimeEquals(expected, submitted))
            {
                return true;
            }
        }

        return false;
    }

    private long GetStep(DateTime utcNow) =>
        (long)(utcNow - DateTime.UnixEpoch).TotalSeconds / _options.TotpStepSeconds;

    private string ComputeCode(byte[] secretBytes, long step)
    {
        var stepBytes = BitConverter.GetBytes(step);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(stepBytes);
        }

        using var hmac = new HMACSHA1(secretBytes);
        var hash = hmac.ComputeHash(stepBytes);

        var offset = hash[^1] & 0x0F;
        var binaryCode = ((hash[offset] & 0x7F) << 24)
            | ((hash[offset + 1] & 0xFF) << 16)
            | ((hash[offset + 2] & 0xFF) << 8)
            | (hash[offset + 3] & 0xFF);

        var code = binaryCode % (int)Math.Pow(10, _options.TotpDigits);
        return code.ToString(new string('0', _options.TotpDigits));
    }

    private static string Base32Encode(byte[] data)
    {
        var output = new StringBuilder((data.Length * 8 + 4) / 5);
        int bitBuffer = 0, bitCount = 0;

        foreach (var b in data)
        {
            bitBuffer = (bitBuffer << 8) | b;
            bitCount += 8;

            while (bitCount >= 5)
            {
                bitCount -= 5;
                output.Append(Base32Alphabet[(bitBuffer >> bitCount) & 0x1F]);
            }
        }

        if (bitCount > 0)
        {
            output.Append(Base32Alphabet[(bitBuffer << (5 - bitCount)) & 0x1F]);
        }

        return output.ToString();
    }

    private static byte[] Base32Decode(string base32)
    {
        var cleaned = base32.Trim().TrimEnd('=').ToUpperInvariant();
        var output = new List<byte>(cleaned.Length * 5 / 8);
        int bitBuffer = 0, bitCount = 0;

        foreach (var c in cleaned)
        {
            var index = Base32Alphabet.IndexOf(c);
            if (index < 0)
            {
                continue;
            }

            bitBuffer = (bitBuffer << 5) | index;
            bitCount += 5;

            if (bitCount >= 8)
            {
                bitCount -= 8;
                output.Add((byte)((bitBuffer >> bitCount) & 0xFF));
            }
        }

        return output.ToArray();
    }
}
