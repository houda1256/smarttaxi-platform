using SmartTaxi.Domain.RoadsideAssistance.Enums;
using SmartTaxi.Domain.RoadsideAssistance.Policies;

namespace SmartTaxi.Domain.Tests.RoadsideAssistance.Policies;

public class RoadsideServiceTypePolicyTests
{
    [Theory]
    [InlineData(RoadsideServiceType.Towing, true)]
    [InlineData(RoadsideServiceType.MechanicalBreakdownAssistance, true)]
    [InlineData(RoadsideServiceType.AccidentAssistance, true)]
    [InlineData(RoadsideServiceType.BatteryJumpStart, false)]
    [InlineData(RoadsideServiceType.TireReplacement, false)]
    [InlineData(RoadsideServiceType.FuelDelivery, false)]
    [InlineData(RoadsideServiceType.Unlocking, false)]
    public void RequiresVehicleImmobilization_ReturnsExpectedValue(RoadsideServiceType serviceType, bool expected)
    {
        Assert.Equal(expected, RoadsideServiceTypePolicy.RequiresVehicleImmobilization(serviceType));
    }
}
