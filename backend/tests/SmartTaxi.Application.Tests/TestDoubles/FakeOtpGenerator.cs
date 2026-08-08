using SmartTaxi.Application.Identity.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeOtpGenerator : IOtpGenerator
{
    private int _counter;

    public string? LastGenerated { get; private set; }

    public string Generate(int digits)
    {
        LastGenerated = (Interlocked.Increment(ref _counter)).ToString().PadLeft(digits, '0');
        return LastGenerated;
    }
}
