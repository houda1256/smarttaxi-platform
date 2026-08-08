using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.BusinessCustomers.Abstractions;
using SmartTaxi.Domain.Payments.BusinessCustomers.Entities;

namespace SmartTaxi.Application.Payments.BusinessCustomers.Commands.RegisterBusinessCustomer;

public sealed class RegisterBusinessCustomerCommandHandler : ICommandHandler<RegisterBusinessCustomerCommand, Result<Guid>>
{
    private readonly IBusinessCustomerRepository _businessCustomerRepository;

    public RegisterBusinessCustomerCommandHandler(IBusinessCustomerRepository businessCustomerRepository)
    {
        _businessCustomerRepository = businessCustomerRepository;
    }

    public async Task<Result<Guid>> Handle(RegisterBusinessCustomerCommand command, CancellationToken cancellationToken)
    {
        BusinessCustomer customer;

        try
        {
            customer = BusinessCustomer.Register(
                command.LegalName, command.TaxIdentifier, command.BillingAddress, command.ContactPersonName,
                command.ContactPersonEmail, command.ContactPersonPhone, command.PaymentTerms, command.CreditLimit, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        await _businessCustomerRepository.AddAsync(customer, cancellationToken);

        return Result<Guid>.Success(customer.Id);
    }
}
