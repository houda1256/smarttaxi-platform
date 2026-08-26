using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Application.Advertising.Queries.GetMyAdvertiserProfile;

public sealed class GetMyAdvertiserProfileQueryHandler : IQueryHandler<GetMyAdvertiserProfileQuery, AdvertiserProfile?>
{
    private readonly IAdvertiserProfileRepository _repository;

    public GetMyAdvertiserProfileQueryHandler(IAdvertiserProfileRepository repository)
    {
        _repository = repository;
    }

    public Task<AdvertiserProfile?> Handle(GetMyAdvertiserProfileQuery query, CancellationToken cancellationToken) =>
        _repository.GetByUserIdAsync(query.UserId, cancellationToken);
}
