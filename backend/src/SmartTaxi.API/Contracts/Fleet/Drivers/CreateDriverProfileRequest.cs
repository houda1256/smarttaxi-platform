namespace SmartTaxi.API.Contracts.Fleet.Drivers;

public sealed record CreateDriverProfileRequest(
    string DriverLicenseNumber, DateTime DriverLicenseExpiration, string? TaxiLicenseNumber, bool IndependentDriver);
