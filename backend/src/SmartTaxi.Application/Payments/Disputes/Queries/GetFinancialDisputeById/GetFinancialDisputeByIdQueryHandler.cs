using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Disputes.Abstractions;
using SmartTaxi.Domain.Payments.Disputes.Entities;

namespace SmartTaxi.Application.Payments.Disputes.Queries.GetFinancialDisputeById;

public sealed class GetFinancialDisputeByIdQueryHandler : IQueryHandler<GetFinancialDisputeByIdQuery, Result<FinancialDispute>>
{
    private const string NotFoundError = "Litige financier introuvable.";

    private readonly IFinancialDisputeRepository _disputeRepository;

    public GetFinancialDisputeByIdQueryHandler(IFinancialDisputeRepository disputeRepository)
    {
        _disputeRepository = disputeRepository;
    }

    public async Task<Result<FinancialDispute>> Handle(GetFinancialDisputeByIdQuery query, CancellationToken cancellationToken)
    {
        var dispute = await _disputeRepository.GetByIdAsync(query.DisputeId, cancellationToken);

        return dispute is null
            ? Result<FinancialDispute>.Failure(NotFoundError, ErrorType.NotFound)
            : Result<FinancialDispute>.Success(dispute);
    }
}
