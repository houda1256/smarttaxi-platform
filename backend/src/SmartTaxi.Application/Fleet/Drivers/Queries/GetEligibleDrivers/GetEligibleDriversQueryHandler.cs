using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;

namespace SmartTaxi.Application.Fleet.Drivers.Queries.GetEligibleDrivers;

/// <summary>Approved and currently Available drivers — a candidate list, never an automatic assignment.</summary>
public sealed class GetEligibleDriversQueryHandler : IQueryHandler<GetEligibleDriversQuery, IReadOnlyCollection<DriverProfileSummary>>
{
    private readonly IDriverProfileRepository _repository;

    public GetEligibleDriversQueryHandler(IDriverProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<DriverProfileSummary>> Handle(
        GetEligibleDriversQuery query, CancellationToken cancellationToken)
    {
        var profiles = await _repository.GetEligibleAsync(cancellationToken);

        return profiles.Select(DriverProfileSummary.FromEntity).ToList();
    }
}
