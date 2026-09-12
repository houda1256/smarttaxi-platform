using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.BusinessCustomers.Abstractions;

namespace SmartTaxi.Application.Payments.BusinessCustomers.Commands.CloseBusinessCustomer;

public sealed class CloseBusinessCustomerCommandHandler : ICommandHandler<CloseBusinessCustomerCommand, Result>
{
    private const string NotFoundError = "Client entreprise introuvable.";
    private const string NotCloseableError = "Ce client entreprise ne peut pas être clôturé.";

    private readonly IBusinessCustomerRepository _businessCustomerRepository;

    public CloseBusinessCustomerCommandHandler(IBusinessCustomerRepository businessCustomerRepository)
    {
        _businessCustomerRepository = businessCustomerRepository;
    }

    public async Task<Result> Handle(CloseBusinessCustomerCommand command, CancellationToken cancellationToken)
    {
        var customer = await _businessCustomerRepository.GetByIdAsync(command.BusinessCustomerId, cancellationToken);

        if (customer is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var closed = await _businessCustomerRepository.TryCloseAsync(command.BusinessCustomerId, DateTime.UtcNow, cancellationToken);

        return closed ? Result.Success() : Result.Failure(NotCloseableError, ErrorType.Conflict);
    }
}
