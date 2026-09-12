using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Assignments.Abstractions;

namespace SmartTaxi.Application.Fleet.Assignments.Queries.GetVehicleAssignments;

public sealed class GetVehicleAssignmentsQueryHandler : IQueryHandler<GetVehicleAssignmentsQuery, IReadOnlyCollection<AssignmentSummary>>
{
    private readonly IDriverVehicleAssignmentRepository _repository;

    public GetVehicleAssignmentsQueryHandler(IDriverVehicleAssignmentRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<AssignmentSummary>> Handle(GetVehicleAssignmentsQuery query, CancellationToken cancellationToken)
    {
        var assignments = await _repository.GetForVehicleAsync(query.VehicleId, cancellationToken);

        return assignments.Select(AssignmentSummary.FromEntity).ToList();
    }
}
