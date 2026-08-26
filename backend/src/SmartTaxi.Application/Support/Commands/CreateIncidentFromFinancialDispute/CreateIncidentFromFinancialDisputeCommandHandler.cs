using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Disputes.Abstractions;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Support.Commands.CreateIncidentFromFinancialDispute;

public sealed class CreateIncidentFromFinancialDisputeCommandHandler
    : ICommandHandler<CreateIncidentFromFinancialDisputeCommand, Result<Guid>>
{
    private const string NotFoundError = "Litige financier introuvable.";

    private readonly IFinancialDisputeRepository _disputeRepository;
    private readonly ISupportIncidentReporter _incidentReporter;

    public CreateIncidentFromFinancialDisputeCommandHandler(
        IFinancialDisputeRepository disputeRepository, ISupportIncidentReporter incidentReporter)
    {
        _disputeRepository = disputeRepository;
        _incidentReporter = incidentReporter;
    }

    public async Task<Result<Guid>> Handle(CreateIncidentFromFinancialDisputeCommand command, CancellationToken cancellationToken)
    {
        var dispute = await _disputeRepository.GetByIdAsync(command.DisputeId, cancellationToken);

        if (dispute is null)
        {
            return Result<Guid>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var incidentId = await _incidentReporter.ReportAsync(
            new SupportIncidentReportRequest(
                SupportIncidentType.PaymentIncident, command.Severity, $"Litige financier {dispute.Id}",
                dispute.Description, command.AdminUserId, SupportRelatedEntityType.FinancialDispute, dispute.Id, Latitude: null,
                Longitude: null, DateTime.UtcNow, SourceType: "FinancialDispute", SourceId: dispute.Id),
            cancellationToken);

        return Result<Guid>.Success(incidentId);
    }
}
