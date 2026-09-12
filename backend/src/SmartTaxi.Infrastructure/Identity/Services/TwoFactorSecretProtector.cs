using Microsoft.AspNetCore.DataProtection;
using SmartTaxi.Application.Identity.Abstractions;

namespace SmartTaxi.Infrastructure.Identity.Services;

internal sealed class TwoFactorSecretProtector : ITwoFactorSecretProtector
{
    private readonly IDataProtector _protector;

    public TwoFactorSecretProtector(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("SmartTaxi.Identity.TwoFactorSecret.v1");
    }

    public string Protect(string rawSecret) => _protector.Protect(rawSecret);

    public string Unprotect(string protectedSecret) => _protector.Unprotect(protectedSecret);
}
