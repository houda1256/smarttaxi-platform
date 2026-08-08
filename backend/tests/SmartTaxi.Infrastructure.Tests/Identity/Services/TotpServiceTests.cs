using Microsoft.Extensions.Options;
using SmartTaxi.Infrastructure.Identity.Options;
using SmartTaxi.Infrastructure.Identity.Services;

namespace SmartTaxi.Infrastructure.Tests.Identity.Services;

public class TotpServiceTests
{
    private static TotpService CreateService(TwoFactorOptions? options = null) =>
        new(Options.Create(options ?? new TwoFactorOptions()));

    [Fact]
    public void GenerateSecret_ProducesDifferentValuesEachTime()
    {
        var service = CreateService();

        var first = service.GenerateSecret();
        var second = service.GenerateSecret();

        Assert.NotEqual(first, second);
        Assert.NotEmpty(first);
    }

    [Fact]
    public void ValidateCode_WithCorrectCodeForCurrentStep_ReturnsTrue()
    {
        var service = CreateService();
        var secret = service.GenerateSecret();
        var utcNow = DateTime.UtcNow;

        // Derive the expected code the same way ValidateCode would, by brute
        // forcing the 6-digit space is impractical here — instead, round-trip
        // through the public API: validate that the code embedded in the
        // authenticator URI's algorithm produces a code ValidateCode accepts.
        var code = ComputeReferenceCode(secret, utcNow, options: new TwoFactorOptions());

        Assert.True(service.ValidateCode(secret, code, utcNow));
    }

    [Fact]
    public void ValidateCode_WithWrongCode_ReturnsFalse()
    {
        var service = CreateService();
        var secret = service.GenerateSecret();
        var utcNow = DateTime.UtcNow;
        var correctCode = ComputeReferenceCode(secret, utcNow, new TwoFactorOptions());
        var wrongCode = correctCode == "000000" ? "111111" : "000000";

        Assert.False(service.ValidateCode(secret, wrongCode, utcNow));
    }

    [Fact]
    public void ValidateCode_OneStepInThePast_IsAcceptedWithinDriftTolerance()
    {
        var options = new TwoFactorOptions();
        var service = CreateService(options);
        var secret = service.GenerateSecret();
        var utcNow = DateTime.UtcNow;
        var codeForPreviousStep = ComputeReferenceCode(secret, utcNow.AddSeconds(-options.TotpStepSeconds), options);

        Assert.True(service.ValidateCode(secret, codeForPreviousStep, utcNow));
    }

    [Fact]
    public void ValidateCode_FarOutsideDriftTolerance_IsRejected()
    {
        var options = new TwoFactorOptions();
        var service = CreateService(options);
        var secret = service.GenerateSecret();
        var utcNow = DateTime.UtcNow;
        var farFutureCode = ComputeReferenceCode(secret, utcNow.AddSeconds(options.TotpStepSeconds * 10), options);

        Assert.False(service.ValidateCode(secret, farFutureCode, utcNow));
    }

    [Fact]
    public void BuildAuthenticatorUri_ContainsExpectedParameters()
    {
        var service = CreateService();
        var secret = service.GenerateSecret();

        var uri = service.BuildAuthenticatorUri(secret, "user@example.com", "SmartTaxi");

        Assert.StartsWith("otpauth://totp/", uri);
        Assert.Contains($"secret={secret}", uri);
        Assert.Contains("issuer=SmartTaxi", uri);
    }

    // Independent reference implementation of RFC 6238, used only to derive an
    // expected code for a known secret/time so the tests don't need to guess —
    // this deliberately duplicates the algorithm rather than calling private
    // members of TotpService, to keep the test a genuine external check.
    private static string ComputeReferenceCode(string base32Secret, DateTime utcNow, TwoFactorOptions options)
    {
        var secretBytes = Base32Decode(base32Secret);
        var step = (long)(utcNow - DateTime.UnixEpoch).TotalSeconds / options.TotpStepSeconds;
        var stepBytes = BitConverter.GetBytes(step);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(stepBytes);
        }

        using var hmac = new System.Security.Cryptography.HMACSHA1(secretBytes);
        var hash = hmac.ComputeHash(stepBytes);
        var offset = hash[^1] & 0x0F;
        var binaryCode = ((hash[offset] & 0x7F) << 24)
            | ((hash[offset + 1] & 0xFF) << 16)
            | ((hash[offset + 2] & 0xFF) << 8)
            | (hash[offset + 3] & 0xFF);
        var code = binaryCode % (int)Math.Pow(10, options.TotpDigits);
        return code.ToString(new string('0', options.TotpDigits));
    }

    private static byte[] Base32Decode(string base32)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var cleaned = base32.Trim().TrimEnd('=').ToUpperInvariant();
        var output = new List<byte>(cleaned.Length * 5 / 8);
        int bitBuffer = 0, bitCount = 0;

        foreach (var c in cleaned)
        {
            var index = alphabet.IndexOf(c);
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
