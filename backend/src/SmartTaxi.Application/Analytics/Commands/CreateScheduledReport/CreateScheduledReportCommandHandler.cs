using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Analytics.Entities;

namespace SmartTaxi.Application.Analytics.Commands.CreateScheduledReport;

public sealed class CreateScheduledReportCommandHandler : ICommandHandler<CreateScheduledReportCommand, Result<Guid>>
{
    private readonly IScheduledReportRepository _repository;

    public CreateScheduledReportCommandHandler(IScheduledReportRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(CreateScheduledReportCommand command, CancellationToken cancellationToken)
    {
        ScheduledReportDefinition definition;

        try
        {
            definition = ScheduledReportDefinition.Create(
                command.Category, command.Frequency, command.RecipientUserId, command.FirstRunAtUtc, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        await _repository.TryAddAsync(definition, cancellationToken);
        return Result<Guid>.Success(definition.Id);
    }
}
