using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.CashDeclarations.Abstractions;

namespace SmartTaxi.Application.Payments.CashDeclarations.Commands.SettleCashDeclaration;

public sealed class SettleCashDeclarationCommandHandler : ICommandHandler<SettleCashDeclarationCommand, Result>
{
    private const string NotFoundError = "Déclaration de caisse introuvable.";
    private const string NotSettleableError = "Cette déclaration ne peut pas être réglée.";

    private readonly ICashDeclarationRepository _declarationRepository;

    public SettleCashDeclarationCommandHandler(ICashDeclarationRepository declarationRepository)
    {
        _declarationRepository = declarationRepository;
    }

    public async Task<Result> Handle(SettleCashDeclarationCommand command, CancellationToken cancellationToken)
    {
        var declaration = await _declarationRepository.GetByIdAsync(command.CashDeclarationId, cancellationToken);

        if (declaration is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var settled = await _declarationRepository.TrySettleAsync(command.CashDeclarationId, DateTime.UtcNow, cancellationToken);

        return settled ? Result.Success() : Result.Failure(NotSettleableError, ErrorType.Conflict);
    }
}
