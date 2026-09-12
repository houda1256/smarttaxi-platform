using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Application.Rides.Commands.SubmitRideComplaint;

public sealed class SubmitRideComplaintCommandHandler : ICommandHandler<SubmitRideComplaintCommand, Result<Guid>>
{
    private const string NotFoundError = "Course introuvable.";
    private const string NoDriverError = "Aucun chauffeur assigné à cette course.";
    private const string NotParticipantError = "Seuls les participants à la course peuvent déposer une réclamation.";

    private readonly IRideRepository _rideRepository;
    private readonly IDriverProfileRepository _driverRepository;
    private readonly IRideComplaintRepository _complaintRepository;

    public SubmitRideComplaintCommandHandler(
        IRideRepository rideRepository, IDriverProfileRepository driverRepository, IRideComplaintRepository complaintRepository)
    {
        _rideRepository = rideRepository;
        _driverRepository = driverRepository;
        _complaintRepository = complaintRepository;
    }

    public async Task<Result<Guid>> Handle(SubmitRideComplaintCommand command, CancellationToken cancellationToken)
    {
        var ride = await _rideRepository.GetByIdAsync(command.RideId, cancellationToken);

        if (ride is null)
        {
            return Result<Guid>.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (ride.SelectedDriverId is null)
        {
            return Result<Guid>.Failure(NoDriverError, ErrorType.Validation);
        }

        var driver = await _driverRepository.GetByIdAsync(ride.SelectedDriverId.Value, cancellationToken);

        if (driver is null)
        {
            return Result<Guid>.Failure(NotFoundError, ErrorType.NotFound);
        }

        Guid concernedUserId;

        if (command.RequestingUserId == ride.CustomerId)
        {
            concernedUserId = driver.UserId;
        }
        else if (command.RequestingUserId == driver.UserId)
        {
            concernedUserId = ride.CustomerId;
        }
        else
        {
            return Result<Guid>.Failure(NotParticipantError, ErrorType.Forbidden);
        }

        RideComplaint complaint;

        try
        {
            complaint = RideComplaint.Submit(
                ride.Id, command.RequestingUserId, concernedUserId, command.Category, command.Description, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        await _complaintRepository.AddAsync(complaint, cancellationToken);

        return Result<Guid>.Success(complaint.Id);
    }
}
