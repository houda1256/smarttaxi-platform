using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.BusinessCustomers.Abstractions;

namespace SmartTaxi.Application.Payments.BusinessCustomers.Commands.SuspendBusinessCustomer;

public sealed class SuspendBusinessCustomerCommandHandler : ICommandHandler<SuspendBusinessCustomerCommand, Result>
{
    private const string NotFoundError = "Client entreprise introuvable.";
    private const string NotSuspendableError = "Ce client entreprise ne peut pas être suspendu.";

    private readonly IBusinessCustomerRepository _businessCustomerRepository;

    public SuspendBusinessCustomerCommandHandler(IBusinessCustomerRepository businessCustomerRepository)
    {
        _businessCustomerRepository = businessCustomerRepository;
    }

    public async Task<Result> Handle(SuspendBusinessCustomerCommand command, CancellationToken cancellationToken)
    {
        var customer = await _businessCustomerRepository.GetByIdAsync(command.BusinessCustomerId, cancellationToken);

        if (customer is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var suspended = await _businessCustomerRepository.TrySuspendAsync(command.BusinessCustomerId, DateTime.UtcNow, cancellationToken);

        return suspended ? Result.Success() : Result.Failure(NotSuspendableError, ErrorType.Conflict);
    }
}
