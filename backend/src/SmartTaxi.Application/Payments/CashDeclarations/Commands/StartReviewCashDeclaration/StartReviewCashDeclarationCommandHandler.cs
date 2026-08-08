using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.CashDeclarations.Abstractions;

namespace SmartTaxi.Application.Payments.CashDeclarations.Commands.StartReviewCashDeclaration;

public sealed class StartReviewCashDeclarationCommandHandler : ICommandHandler<StartReviewCashDeclarationCommand, Result>
{
    private const string NotFoundError = "Déclaration de caisse introuvable.";
    private const string NotReviewableError = "Cette déclaration ne peut pas être mise en revue.";

    private readonly ICashDeclarationRepository _declarationRepository;

    public StartReviewCashDeclarationCommandHandler(ICashDeclarationRepository declarationRepository)
    {
        _declarationRepository = declarationRepository;
    }

    public async Task<Result> Handle(StartReviewCashDeclarationCommand command, CancellationToken cancellationToken)
    {
        var declaration = await _declarationRepository.GetByIdAsync(command.CashDeclarationId, cancellationToken);

        if (declaration is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var started = await _declarationRepository.TryStartReviewAsync(command.CashDeclarationId, DateTime.UtcNow, cancellationToken);

        return started ? Result.Success() : Result.Failure(NotReviewableError, ErrorType.Conflict);
    }
}
