using SmartTaxi.Application.Identity.Referrals.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeReferralCodeGenerator : IReferralCodeGenerator
{
    private int _counter;

    public string Generate() => $"CODE{Interlocked.Increment(ref _counter)}";
}
