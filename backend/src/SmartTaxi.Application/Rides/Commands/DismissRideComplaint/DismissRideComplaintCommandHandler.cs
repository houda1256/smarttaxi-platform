using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Application.Rides.Commands.DismissRideComplaint;

public sealed class DismissRideComplaintCommandHandler : ICommandHandler<DismissRideComplaintCommand, Result>
{
    private const string NotFoundError = "Réclamation introuvable ou déjà traitée.";

    private readonly IRideComplaintRepository _complaintRepository;

    public DismissRideComplaintCommandHandler(IRideComplaintRepository complaintRepository)
    {
        _complaintRepository = complaintRepository;
    }

    public async Task<Result> Handle(DismissRideComplaintCommand command, CancellationToken cancellationToken)
    {
        var dismissed = await _complaintRepository.TryDismissAsync(
            command.ComplaintId, command.Resolution, DateTime.UtcNow, cancellationToken);

        return dismissed ? Result.Success() : Result.Failure(NotFoundError, ErrorType.Conflict);
    }
}
