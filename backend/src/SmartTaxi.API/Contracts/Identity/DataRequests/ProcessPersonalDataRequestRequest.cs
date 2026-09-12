namespace SmartTaxi.API.Contracts.Identity.DataRequests;

public sealed record ProcessPersonalDataRequestRequest(bool Approve, string? ProcessingNotes);
