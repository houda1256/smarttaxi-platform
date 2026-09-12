using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.CashDeclarations.Abstractions;

namespace SmartTaxi.Application.Payments.CashDeclarations.Commands.ApproveCashDeclaration;

public sealed class ApproveCashDeclarationCommandHandler : ICommandHandler<ApproveCashDeclarationCommand, Result>
{
    private const string NotFoundError = "Déclaration de caisse introuvable.";
    private const string NotApprovableError = "Cette déclaration ne peut plus être approuvée.";

    private readonly ICashDeclarationRepository _declarationRepository;

    public ApproveCashDeclarationCommandHandler(ICashDeclarationRepository declarationRepository)
    {
        _declarationRepository = declarationRepository;
    }

    public async Task<Result> Handle(ApproveCashDeclarationCommand command, CancellationToken cancellationToken)
    {
        var declaration = await _declarationRepository.GetByIdAsync(command.CashDeclarationId, cancellationToken);

        if (declaration is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var approved = await _declarationRepository.TryApproveAsync(command.CashDeclarationId, command.ReviewedBy, DateTime.UtcNow, cancellationToken);

        return approved ? Result.Success() : Result.Failure(NotApprovableError, ErrorType.Conflict);
    }
}
