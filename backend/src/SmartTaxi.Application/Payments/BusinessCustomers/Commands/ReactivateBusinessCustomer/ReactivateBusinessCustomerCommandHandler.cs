using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.BusinessCustomers.Abstractions;

namespace SmartTaxi.Application.Payments.BusinessCustomers.Commands.ReactivateBusinessCustomer;

public sealed class ReactivateBusinessCustomerCommandHandler : ICommandHandler<ReactivateBusinessCustomerCommand, Result>
{
    private const string NotFoundError = "Client entreprise introuvable.";
    private const string NotReactivableError = "Ce client entreprise ne peut pas être réactivé.";

    private readonly IBusinessCustomerRepository _businessCustomerRepository;

    public ReactivateBusinessCustomerCommandHandler(IBusinessCustomerRepository businessCustomerRepository)
    {
        _businessCustomerRepository = businessCustomerRepository;
    }

    public async Task<Result> Handle(ReactivateBusinessCustomerCommand command, CancellationToken cancellationToken)
    {
        var customer = await _businessCustomerRepository.GetByIdAsync(command.BusinessCustomerId, cancellationToken);

        if (customer is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var reactivated = await _businessCustomerRepository.TryReactivateAsync(command.BusinessCustomerId, DateTime.UtcNow, cancellationToken);

        return reactivated ? Result.Success() : Result.Failure(NotReactivableError, ErrorType.Conflict);
    }
}
