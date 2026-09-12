using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Domain.RoadsideAssistance.Entities;

namespace SmartTaxi.Application.RoadsideAssistance.Queries.GetMyRoadsidePartnerProfile;

public sealed class GetMyRoadsidePartnerProfileQueryHandler : IQueryHandler<GetMyRoadsidePartnerProfileQuery, RoadsidePartnerProfile?>
{
    private readonly IRoadsidePartnerProfileRepository _repository;

    public GetMyRoadsidePartnerProfileQueryHandler(IRoadsidePartnerProfileRepository repository)
    {
        _repository = repository;
    }

    public Task<RoadsidePartnerProfile?> Handle(GetMyRoadsidePartnerProfileQuery query, CancellationToken cancellationToken) =>
        _repository.GetByUserIdAsync(query.UserId, cancellationToken);
}
