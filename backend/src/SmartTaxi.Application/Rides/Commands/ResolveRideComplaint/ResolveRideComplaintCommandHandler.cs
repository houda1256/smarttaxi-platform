using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Application.Rides.Commands.ResolveRideComplaint;

public sealed class ResolveRideComplaintCommandHandler : ICommandHandler<ResolveRideComplaintCommand, Result>
{
    private const string NotFoundError = "Réclamation introuvable ou déjà traitée.";

    private readonly IRideComplaintRepository _complaintRepository;

    public ResolveRideComplaintCommandHandler(IRideComplaintRepository complaintRepository)
    {
        _complaintRepository = complaintRepository;
    }

    public async Task<Result> Handle(ResolveRideComplaintCommand command, CancellationToken cancellationToken)
    {
        var resolved = await _complaintRepository.TryResolveAsync(
            command.ComplaintId, command.Resolution, DateTime.UtcNow, cancellationToken);

        return resolved ? Result.Success() : Result.Failure(NotFoundError, ErrorType.Conflict);
    }
}
