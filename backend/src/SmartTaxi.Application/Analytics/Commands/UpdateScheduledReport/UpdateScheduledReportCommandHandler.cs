using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Analytics.Commands.UpdateScheduledReport;

public sealed class UpdateScheduledReportCommandHandler : ICommandHandler<UpdateScheduledReportCommand, Result>
{
    private const string NotFoundError = "Rapport planifié introuvable.";

    private readonly IScheduledReportRepository _repository;

    public UpdateScheduledReportCommandHandler(IScheduledReportRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(UpdateScheduledReportCommand command, CancellationToken cancellationToken)
    {
        var updated = await _repository.TryUpdateAsync(
            command.Id, command.Category, command.Frequency, command.RecipientUserId, DateTime.UtcNow, cancellationToken);

        return updated ? Result.Success() : Result.Failure(NotFoundError, ErrorType.NotFound);
    }
}
