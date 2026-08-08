using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Professional.Abstractions;

namespace SmartTaxi.Application.Identity.Professional.Queries.GetProfessionalAccountRequestByIdAdmin;

public sealed class GetProfessionalAccountRequestByIdAdminQueryHandler
    : IQueryHandler<GetProfessionalAccountRequestByIdAdminQuery, Result<ProfessionalAccountRequestSummary>>
{
    private const string NotFoundError = "Demande introuvable.";

    private readonly IProfessionalAccountRequestRepository _repository;

    public GetProfessionalAccountRequestByIdAdminQueryHandler(IProfessionalAccountRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ProfessionalAccountRequestSummary>> Handle(
        GetProfessionalAccountRequestByIdAdminQuery query, CancellationToken cancellationToken)
    {
        var request = await _repository.GetByIdAsync(query.RequestId, cancellationToken);

        if (request is null)
        {
            return Result<ProfessionalAccountRequestSummary>.Failure(NotFoundError, ErrorType.NotFound);
        }

        return Result<ProfessionalAccountRequestSummary>.Success(ProfessionalAccountRequestSummary.FromEntity(request));
    }
}
