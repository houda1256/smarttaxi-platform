using System.Security.Cryptography;
using System.Text;
using SmartTaxi.Application.Identity.Abstractions;

namespace SmartTaxi.Infrastructure.Identity.Services;

internal sealed class OtpGenerator : IOtpGenerator
{
    public string Generate(int digits)
    {
        var builder = new StringBuilder(digits);

        for (var i = 0; i < digits; i++)
        {
            builder.Append(RandomNumberGenerator.GetInt32(0, 10));
        }

        return builder.ToString();
    }
}
