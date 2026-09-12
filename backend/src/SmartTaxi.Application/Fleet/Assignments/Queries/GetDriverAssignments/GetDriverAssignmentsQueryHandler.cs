using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Assignments.Abstractions;

namespace SmartTaxi.Application.Fleet.Assignments.Queries.GetDriverAssignments;

public sealed class GetDriverAssignmentsQueryHandler : IQueryHandler<GetDriverAssignmentsQuery, IReadOnlyCollection<AssignmentSummary>>
{
    private readonly IDriverVehicleAssignmentRepository _repository;

    public GetDriverAssignmentsQueryHandler(IDriverVehicleAssignmentRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<AssignmentSummary>> Handle(GetDriverAssignmentsQuery query, CancellationToken cancellationToken)
    {
        var assignments = await _repository.GetForDriverAsync(query.DriverId, cancellationToken);

        return assignments.Select(AssignmentSummary.FromEntity).ToList();
    }
}
