using SmartTaxi.Domain.Identity.DataRequests.Enums;

namespace SmartTaxi.Domain.Identity.DataRequests.Entities;

public sealed class PersonalDataRequest
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public PersonalDataRequestType RequestType { get; private set; }
    public PersonalDataRequestStatus Status { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public Guid? ProcessedBy { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? ProcessingNotes { get; private set; }
    public string? ResultReference { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private PersonalDataRequest()
    {
    }

    public PersonalDataRequest(Guid userId, PersonalDataRequestType requestType, DateTime utcNow)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        RequestType = requestType;
        Status = PersonalDataRequestStatus.Pending;
        RequestedAt = utcNow;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }
}
