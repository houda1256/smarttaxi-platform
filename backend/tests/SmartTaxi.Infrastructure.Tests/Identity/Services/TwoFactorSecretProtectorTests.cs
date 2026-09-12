using Microsoft.Extensions.DependencyInjection;
using SmartTaxi.Infrastructure.Identity.Services;

namespace SmartTaxi.Infrastructure.Tests.Identity.Services;

public class TwoFactorSecretProtectorTests
{
    private static TwoFactorSecretProtector CreateProtector()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();
        var provider = services.BuildServiceProvider();
        return new TwoFactorSecretProtector(provider.GetRequiredService<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>());
    }

    [Fact]
    public void Protect_ThenUnprotect_RoundTripsToTheOriginalSecret()
    {
        var protector = CreateProtector();
        const string rawSecret = "JBSWY3DPEHPK3PXP";

        var protectedValue = protector.Protect(rawSecret);
        var unprotectedValue = protector.Unprotect(protectedValue);

        Assert.Equal(rawSecret, unprotectedValue);
    }

    [Fact]
    public void Protect_NeverReturnsThePlaintextSecret()
    {
        var protector = CreateProtector();
        const string rawSecret = "JBSWY3DPEHPK3PXP";

        var protectedValue = protector.Protect(rawSecret);

        Assert.DoesNotContain(rawSecret, protectedValue);
    }
}
