using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Documents.Abstractions;

namespace SmartTaxi.Application.Identity.Documents.Commands.ExpireDocuments;

public sealed class ExpireDocumentsCommandHandler : ICommandHandler<ExpireDocumentsCommand, Result<ExpireDocumentsResult>>
{
    private readonly IUserDocumentRepository _repository;

    public ExpireDocumentsCommandHandler(IUserDocumentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ExpireDocumentsResult>> Handle(ExpireDocumentsCommand command, CancellationToken cancellationToken)
    {
        var count = await _repository.ExpireDueDocumentsAsync(DateTime.UtcNow, cancellationToken);

        return Result<ExpireDocumentsResult>.Success(new ExpireDocumentsResult(count));
    }
}
