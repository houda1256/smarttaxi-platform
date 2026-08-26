using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Fleet.Assignments.Abstractions;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Application.Identity.Professional.Abstractions;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Application.Payments.Abstractions;
using SmartTaxi.Application.Payments.Disputes.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Application.Subscriptions.Abstractions;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Fleet.Assignments.Enums;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Support;

/// <summary>
/// One branch per closed SupportRelatedEntityType value, each a read-only
/// call into the real owning module's own existing repository — never a
/// second copy of ownership logic, never a write to any of these modules.
/// </summary>
public sealed class SupportRelatedEntityValidator : ISupportRelatedEntityValidator
{
    private readonly IRideRepository _rideRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IFinancialDisputeRepository _financialDisputeRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IAdCampaignRepository _adCampaignRepository;
    private readonly IMaintenanceRequestRepository _maintenanceRequestRepository;
    private readonly IRoadsideAssistanceRequestRepository _roadsideAssistanceRequestRepository;
    private readonly IProfessionalAccountRequestRepository _professionalAccountRequestRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IDriverVehicleAssignmentRepository _driverVehicleAssignmentRepository;

    public SupportRelatedEntityValidator(
        IRideRepository rideRepository, IPaymentRepository paymentRepository, IFinancialDisputeRepository financialDisputeRepository,
        ISubscriptionRepository subscriptionRepository, IAdCampaignRepository adCampaignRepository,
        IMaintenanceRequestRepository maintenanceRequestRepository, IRoadsideAssistanceRequestRepository roadsideAssistanceRequestRepository,
        IProfessionalAccountRequestRepository professionalAccountRequestRepository, IVehicleRepository vehicleRepository,
        IDriverVehicleAssignmentRepository driverVehicleAssignmentRepository)
    {
        _rideRepository = rideRepository;
        _paymentRepository = paymentRepository;
        _financialDisputeRepository = financialDisputeRepository;
        _subscriptionRepository = subscriptionRepository;
        _adCampaignRepository = adCampaignRepository;
        _maintenanceRequestRepository = maintenanceRequestRepository;
        _roadsideAssistanceRequestRepository = roadsideAssistanceRequestRepository;
        _professionalAccountRequestRepository = professionalAccountRequestRepository;
        _vehicleRepository = vehicleRepository;
        _driverVehicleAssignmentRepository = driverVehicleAssignmentRepository;
    }

    public async Task<bool> IsValidReferenceAsync(
        SupportRelatedEntityType type, Guid entityId, Guid callerUserId, CancellationToken cancellationToken)
    {
        switch (type)
        {
            case SupportRelatedEntityType.Ride:
                var ride = await _rideRepository.GetByIdAsync(entityId, cancellationToken);
                return ride is not null && (ride.CustomerId == callerUserId || ride.SelectedDriverId == callerUserId);

            case SupportRelatedEntityType.Payment:
                var payment = await _paymentRepository.GetByIdAsync(entityId, cancellationToken);
                return payment is not null && payment.CustomerId == callerUserId;

            case SupportRelatedEntityType.FinancialDispute:
                var dispute = await _financialDisputeRepository.GetByIdAsync(entityId, cancellationToken);
                return dispute is not null && dispute.RaisedBy == callerUserId;

            case SupportRelatedEntityType.Subscription:
                var subscription = await _subscriptionRepository.GetByIdAsync(entityId, cancellationToken);
                return subscription is not null && subscription.SubscriberId == callerUserId;

            case SupportRelatedEntityType.AdvertisingCampaign:
                var campaign = await _adCampaignRepository.GetByIdAsync(entityId, cancellationToken);
                return campaign is not null && campaign.AdvertiserUserId == callerUserId;

            case SupportRelatedEntityType.MaintenanceRequest:
                var maintenanceRequest = await _maintenanceRequestRepository.GetByIdAsync(entityId, cancellationToken);
                return maintenanceRequest is not null
                    && (maintenanceRequest.OwnerUserId == callerUserId || maintenanceRequest.GarageUserId == callerUserId);

            case SupportRelatedEntityType.RoadsideAssistanceRequest:
                var roadsideRequest = await _roadsideAssistanceRequestRepository.GetByIdAsync(entityId, cancellationToken);
                return roadsideRequest is not null
                    && (roadsideRequest.RequesterUserId == callerUserId || roadsideRequest.SelectedPartnerUserId == callerUserId);

            case SupportRelatedEntityType.ProfessionalAccountRequest:
                var professionalRequest = await _professionalAccountRequestRepository.GetByIdAsync(entityId, cancellationToken);
                return professionalRequest is not null && professionalRequest.UserId == callerUserId;

            case SupportRelatedEntityType.Vehicle:
                var vehicle = await _vehicleRepository.GetByIdAsync(entityId, cancellationToken);

                if (vehicle is null)
                {
                    return false;
                }

                if (vehicle.OwnerId == callerUserId)
                {
                    return true;
                }

                var assignments = await _driverVehicleAssignmentRepository.GetActiveOrPendingForDriverAsync(callerUserId, null, cancellationToken);
                return assignments.Any(assignment => assignment.VehicleId == entityId && assignment.Status == AssignmentStatus.Active);

            default:
                return false;
        }
    }
}
