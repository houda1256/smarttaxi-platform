using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Application.Rides.Queries.GetRideComplaints;

/// <summary>Admin/support read — gated by the rides.dispute permission at the API layer, no ownership check here.</summary>
public sealed class GetRideComplaintsQueryHandler : IQueryHandler<GetRideComplaintsQuery, Result<IReadOnlyCollection<RideComplaint>>>
{
    private readonly IRideComplaintRepository _complaintRepository;

    public GetRideComplaintsQueryHandler(IRideComplaintRepository complaintRepository)
    {
        _complaintRepository = complaintRepository;
    }

    public async Task<Result<IReadOnlyCollection<RideComplaint>>> Handle(GetRideComplaintsQuery query, CancellationToken cancellationToken)
    {
        var complaints = await _complaintRepository.GetForRideAsync(query.RideId, cancellationToken);
        return Result<IReadOnlyCollection<RideComplaint>>.Success(complaints);
    }
}
