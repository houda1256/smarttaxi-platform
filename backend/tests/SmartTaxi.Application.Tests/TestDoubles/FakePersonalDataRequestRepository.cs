using SmartTaxi.Application.Identity.DataRequests.Abstractions;
using SmartTaxi.Domain.Identity.DataRequests.Entities;
using SmartTaxi.Domain.Identity.DataRequests.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakePersonalDataRequestRepository : IPersonalDataRequestRepository
{
    private readonly Dictionary<Guid, PersonalDataRequest> _requestsById = new();

    public Task AddAsync(PersonalDataRequest request, CancellationToken cancellationToken)
    {
        _requestsById[request.Id] = request;
        return Task.CompletedTask;
    }

    public Task<PersonalDataRequest?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken) =>
        Task.FromResult(_requestsById.GetValueOrDefault(requestId));

    public Task<IReadOnlyCollection<PersonalDataRequest>> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<PersonalDataRequest> requests = _requestsById.Values.Where(r => r.UserId == userId).ToList();
        return Task.FromResult(requests);
    }

    public Task<IReadOnlyCollection<PersonalDataRequest>> GetPendingAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<PersonalDataRequest> requests =
            _requestsById.Values.Where(r => r.Status == PersonalDataRequestStatus.Pending).ToList();
        return Task.FromResult(requests);
    }

    public Task<PersonalDataRequest?> GetPendingForUserAndTypeAsync(
        Guid userId, PersonalDataRequestType requestType, CancellationToken cancellationToken)
    {
        var pending = _requestsById.Values.FirstOrDefault(r =>
            r.UserId == userId && r.RequestType == requestType && r.Status == PersonalDataRequestStatus.Pending);

        return Task.FromResult(pending);
    }

    public Task<bool> TryCompleteAsync(
        Guid requestId, Guid processedBy, DateTime utcNow, string? processingNotes, string? resultReference,
        CancellationToken cancellationToken)
    {
        if (!_requestsById.TryGetValue(requestId, out var request) || request.Status != PersonalDataRequestStatus.Pending)
        {
            return Task.FromResult(false);
        }

        SetProperty(request, nameof(PersonalDataRequest.Status), PersonalDataRequestStatus.Completed);
        SetProperty(request, nameof(PersonalDataRequest.ProcessedBy), processedBy);
        SetProperty(request, nameof(PersonalDataRequest.ProcessedAt), utcNow);
        SetProperty(request, nameof(PersonalDataRequest.ProcessingNotes), processingNotes);
        SetProperty(request, nameof(PersonalDataRequest.ResultReference), resultReference);
        SetProperty(request, nameof(PersonalDataRequest.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryRejectAsync(
        Guid requestId, Guid processedBy, DateTime utcNow, string? processingNotes, CancellationToken cancellationToken)
    {
        if (!_requestsById.TryGetValue(requestId, out var request) || request.Status != PersonalDataRequestStatus.Pending)
        {
            return Task.FromResult(false);
        }

        SetProperty(request, nameof(PersonalDataRequest.Status), PersonalDataRequestStatus.Rejected);
        SetProperty(request, nameof(PersonalDataRequest.ProcessedBy), processedBy);
        SetProperty(request, nameof(PersonalDataRequest.ProcessedAt), utcNow);
        SetProperty(request, nameof(PersonalDataRequest.ProcessingNotes), processingNotes);
        SetProperty(request, nameof(PersonalDataRequest.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public int Count => _requestsById.Count;

    private static void SetProperty(PersonalDataRequest request, string propertyName, object? value) =>
        typeof(PersonalDataRequest).GetProperty(propertyName)!.SetValue(request, value);
}
