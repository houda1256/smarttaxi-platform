namespace SmartTaxi.Domain.RoadsideAssistance.Enums;

/// <summary>Exact service types named by the business specification's Roadside Assistance section — none invented. See RoadsideServiceTypePolicy for which of these immobilize the vehicle.</summary>
public enum RoadsideServiceType
{
    Towing,
    MechanicalBreakdownAssistance,
    AccidentAssistance,
    BatteryJumpStart,
    TireReplacement,
    FuelDelivery,
    Unlocking
}
