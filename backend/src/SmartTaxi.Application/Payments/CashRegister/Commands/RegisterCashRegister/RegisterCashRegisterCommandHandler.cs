using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.CashRegister.Abstractions;
using SmartTaxi.Domain.Payments.CashRegister.Entities;

namespace SmartTaxi.Application.Payments.CashRegister.Commands.RegisterCashRegister;

public sealed class RegisterCashRegisterCommandHandler : ICommandHandler<RegisterCashRegisterCommand, Result<Guid>>
{
    private readonly ICashRegisterRepository _cashRegisterRepository;

    public RegisterCashRegisterCommandHandler(ICashRegisterRepository cashRegisterRepository)
    {
        _cashRegisterRepository = cashRegisterRepository;
    }

    public async Task<Result<Guid>> Handle(RegisterCashRegisterCommand command, CancellationToken cancellationToken)
    {
        CashRegisterBox cashRegister;

        try
        {
            cashRegister = CashRegisterBox.Register(command.OwnerId, command.Label, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        await _cashRegisterRepository.AddAsync(cashRegister, cancellationToken);

        return Result<Guid>.Success(cashRegister.Id);
    }
}
