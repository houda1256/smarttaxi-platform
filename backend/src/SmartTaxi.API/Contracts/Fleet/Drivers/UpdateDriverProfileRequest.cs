namespace SmartTaxi.API.Contracts.Fleet.Drivers;

public sealed record UpdateDriverProfileRequest(string DriverLicenseNumber, DateTime DriverLicenseExpiration, string? TaxiLicenseNumber);
