using SmartTaxi.Application.Identity.Professional.Abstractions;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.Professional.Entities;
using SmartTaxi.Domain.Identity.Professional.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeProfessionalAccountRequestRepository : IProfessionalAccountRequestRepository
{
    private readonly Dictionary<Guid, ProfessionalAccountRequest> _requestsById = new();

    public Task AddAsync(ProfessionalAccountRequest request, CancellationToken cancellationToken)
    {
        _requestsById[request.Id] = request;
        return Task.CompletedTask;
    }

    public Task<ProfessionalAccountRequest?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken) =>
        Task.FromResult(_requestsById.GetValueOrDefault(requestId));

    public Task<IReadOnlyCollection<ProfessionalAccountRequest>> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ProfessionalAccountRequest> requests =
            _requestsById.Values.Where(r => r.UserId == userId).ToList();
        return Task.FromResult(requests);
    }

    public Task<IReadOnlyCollection<ProfessionalAccountRequest>> GetPendingAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ProfessionalAccountRequest> requests =
            _requestsById.Values.Where(r => r.Status == ProfessionalAccountStatus.PendingReview).ToList();
        return Task.FromResult(requests);
    }

    public Task<ProfessionalAccountRequest?> GetActiveForUserAndRoleAsync(
        Guid userId, UserRole role, CancellationToken cancellationToken)
    {
        var active = _requestsById.Values.FirstOrDefault(r =>
            r.UserId == userId && r.Role == role
            && (r.Status == ProfessionalAccountStatus.PendingReview || r.Status == ProfessionalAccountStatus.Approved));

        return Task.FromResult(active);
    }

    public Task<bool> TryApproveAsync(
        Guid requestId, Guid reviewedBy, DateTime utcNow, string? reviewComment, CancellationToken cancellationToken)
    {
        if (!_requestsById.TryGetValue(requestId, out var request) || request.Status != ProfessionalAccountStatus.PendingReview)
        {
            return Task.FromResult(false);
        }

        SetProperty(request, nameof(ProfessionalAccountRequest.Status), ProfessionalAccountStatus.Approved);
        SetProperty(request, nameof(ProfessionalAccountRequest.ReviewedBy), reviewedBy);
        SetProperty(request, nameof(ProfessionalAccountRequest.ReviewedAt), utcNow);
        SetProperty(request, nameof(ProfessionalAccountRequest.ReviewComment), reviewComment);
        SetProperty(request, nameof(ProfessionalAccountRequest.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryRejectAsync(
        Guid requestId, Guid reviewedBy, DateTime utcNow, string rejectionReason, string? reviewComment,
        CancellationToken cancellationToken)
    {
        if (!_requestsById.TryGetValue(requestId, out var request) || request.Status != ProfessionalAccountStatus.PendingReview)
        {
            return Task.FromResult(false);
        }

        SetProperty(request, nameof(ProfessionalAccountRequest.Status), ProfessionalAccountStatus.Rejected);
        SetProperty(request, nameof(ProfessionalAccountRequest.ReviewedBy), reviewedBy);
        SetProperty(request, nameof(ProfessionalAccountRequest.ReviewedAt), utcNow);
        SetProperty(request, nameof(ProfessionalAccountRequest.RejectionReason), rejectionReason);
        SetProperty(request, nameof(ProfessionalAccountRequest.ReviewComment), reviewComment);
        SetProperty(request, nameof(ProfessionalAccountRequest.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TrySuspendAsync(
        Guid requestId, Guid reviewedBy, DateTime utcNow, string? reviewComment, CancellationToken cancellationToken)
    {
        if (!_requestsById.TryGetValue(requestId, out var request) || request.Status != ProfessionalAccountStatus.Approved)
        {
            return Task.FromResult(false);
        }

        SetProperty(request, nameof(ProfessionalAccountRequest.Status), ProfessionalAccountStatus.Suspended);
        SetProperty(request, nameof(ProfessionalAccountRequest.ReviewedBy), reviewedBy);
        SetProperty(request, nameof(ProfessionalAccountRequest.ReviewedAt), utcNow);
        SetProperty(request, nameof(ProfessionalAccountRequest.ReviewComment), reviewComment);
        SetProperty(request, nameof(ProfessionalAccountRequest.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryReactivateAsync(
        Guid requestId, Guid reviewedBy, DateTime utcNow, string? reviewComment, CancellationToken cancellationToken)
    {
        if (!_requestsById.TryGetValue(requestId, out var request) || request.Status != ProfessionalAccountStatus.Suspended)
        {
            return Task.FromResult(false);
        }

        SetProperty(request, nameof(ProfessionalAccountRequest.Status), ProfessionalAccountStatus.Approved);
        SetProperty(request, nameof(ProfessionalAccountRequest.ReviewedBy), reviewedBy);
        SetProperty(request, nameof(ProfessionalAccountRequest.ReviewedAt), utcNow);
        SetProperty(request, nameof(ProfessionalAccountRequest.ReviewComment), reviewComment);
        SetProperty(request, nameof(ProfessionalAccountRequest.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public int Count => _requestsById.Count;

    private static void SetProperty(ProfessionalAccountRequest request, string propertyName, object? value) =>
        typeof(ProfessionalAccountRequest).GetProperty(propertyName)!.SetValue(request, value);
}
