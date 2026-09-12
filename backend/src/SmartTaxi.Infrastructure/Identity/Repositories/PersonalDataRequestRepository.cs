using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Identity.DataRequests.Abstractions;
using SmartTaxi.Domain.Identity.DataRequests.Entities;
using SmartTaxi.Domain.Identity.DataRequests.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Identity.Repositories;

internal sealed class PersonalDataRequestRepository : IPersonalDataRequestRepository
{
    private readonly ApplicationDbContext _context;

    public PersonalDataRequestRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PersonalDataRequest request, CancellationToken cancellationToken)
    {
        await _context.PersonalDataRequests.AddAsync(request, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<PersonalDataRequest?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken) =>
        _context.PersonalDataRequests.FirstOrDefaultAsync(request => request.Id == requestId, cancellationToken);

    public async Task<IReadOnlyCollection<PersonalDataRequest>> GetForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await _context.PersonalDataRequests.Where(request => request.UserId == userId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<PersonalDataRequest>> GetPendingAsync(CancellationToken cancellationToken) =>
        await _context.PersonalDataRequests
            .Where(request => request.Status == PersonalDataRequestStatus.Pending)
            .ToListAsync(cancellationToken);

    public Task<PersonalDataRequest?> GetPendingForUserAndTypeAsync(
        Guid userId, PersonalDataRequestType requestType, CancellationToken cancellationToken) =>
        _context.PersonalDataRequests.FirstOrDefaultAsync(
            request => request.UserId == userId && request.RequestType == requestType
                && request.Status == PersonalDataRequestStatus.Pending,
            cancellationToken);

    public async Task<bool> TryCompleteAsync(
        Guid requestId, Guid processedBy, DateTime utcNow, string? processingNotes, string? resultReference,
        CancellationToken cancellationToken)
    {
        var rows = await _context.PersonalDataRequests
            .Where(request => request.Id == requestId && request.Status == PersonalDataRequestStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(request => request.Status, PersonalDataRequestStatus.Completed)
                .SetProperty(request => request.ProcessedBy, processedBy)
                .SetProperty(request => request.ProcessedAt, utcNow)
                .SetProperty(request => request.ProcessingNotes, processingNotes)
                .SetProperty(request => request.ResultReference, resultReference)
                .SetProperty(request => request.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryRejectAsync(
        Guid requestId, Guid processedBy, DateTime utcNow, string? processingNotes, CancellationToken cancellationToken)
    {
        var rows = await _context.PersonalDataRequests
            .Where(request => request.Id == requestId && request.Status == PersonalDataRequestStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(request => request.Status, PersonalDataRequestStatus.Rejected)
                .SetProperty(request => request.ProcessedBy, processedBy)
                .SetProperty(request => request.ProcessedAt, utcNow)
                .SetProperty(request => request.ProcessingNotes, processingNotes)
                .SetProperty(request => request.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }
}
