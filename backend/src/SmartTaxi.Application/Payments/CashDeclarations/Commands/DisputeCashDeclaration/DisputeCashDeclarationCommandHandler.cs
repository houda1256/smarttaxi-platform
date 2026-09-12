using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.CashDeclarations.Abstractions;

namespace SmartTaxi.Application.Payments.CashDeclarations.Commands.DisputeCashDeclaration;

public sealed class DisputeCashDeclarationCommandHandler : ICommandHandler<DisputeCashDeclarationCommand, Result>
{
    private const string NotFoundError = "Déclaration de caisse introuvable.";
    private const string NotDisputableError = "Cette déclaration ne peut plus être mise en litige.";

    private readonly ICashDeclarationRepository _declarationRepository;

    public DisputeCashDeclarationCommandHandler(ICashDeclarationRepository declarationRepository)
    {
        _declarationRepository = declarationRepository;
    }

    public async Task<Result> Handle(DisputeCashDeclarationCommand command, CancellationToken cancellationToken)
    {
        var declaration = await _declarationRepository.GetByIdAsync(command.CashDeclarationId, cancellationToken);

        if (declaration is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var disputed = await _declarationRepository.TryDisputeAsync(command.CashDeclarationId, command.ReviewedBy, DateTime.UtcNow, cancellationToken);

        return disputed ? Result.Success() : Result.Failure(NotDisputableError, ErrorType.Conflict);
    }
}
