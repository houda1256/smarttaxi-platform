using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Application.RoadsideAssistance.Contracts;
using SmartTaxi.Domain.Rides.ValueObjects;

namespace SmartTaxi.Application.RoadsideAssistance.Queries.GetRecommendedRoadsidePartners;

/// <summary>
/// Deterministic MVP recommendation — no AI, no external map/route provider,
/// no route optimization (approved plan, §C). Compatibility filtering
/// (active + service type + vehicle category + normalized city) happens in
/// the repository; distance is computed here, via the existing
/// IDistanceCalculator (Haversine, already used by Rides — reused rather than
/// re-implemented), ONLY when both the request and the candidate partner have
/// recorded coordinates — never defaulted or estimated. No rating (no
/// trustworthy field exists), no ETA, no availability/busy state. The
/// requester's own exact coordinates never leave this query's own
/// computation — the DTO returned to the caller carries only the candidate
/// partners' own public City/BusinessName/computed distance, and (per the
/// approved plan) no partner is ever shown this request until manually
/// selected, so no location ever reaches an unrelated partner.
/// </summary>
public sealed class GetRecommendedRoadsidePartnersQueryHandler
    : IQueryHandler<GetRecommendedRoadsidePartnersQuery, Result<IReadOnlyCollection<RecommendedRoadsidePartner>>>
{
    private const string NotFoundError = "Demande d'assistance routière introuvable.";
    private const string ForbiddenError = "Vous n'avez pas accès à cette demande d'assistance routière.";
    private const string VehicleNotFoundError = "Véhicule introuvable.";

    private readonly IRoadsideAssistanceRequestRepository _requestRepository;
    private readonly IRoadsidePartnerProfileRepository _partnerProfileRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IDistanceCalculator _distanceCalculator;

    public GetRecommendedRoadsidePartnersQueryHandler(
        IRoadsideAssistanceRequestRepository requestRepository, IRoadsidePartnerProfileRepository partnerProfileRepository,
        IVehicleRepository vehicleRepository, IDistanceCalculator distanceCalculator)
    {
        _requestRepository = requestRepository;
        _partnerProfileRepository = partnerProfileRepository;
        _vehicleRepository = vehicleRepository;
        _distanceCalculator = distanceCalculator;
    }

    public async Task<Result<IReadOnlyCollection<RecommendedRoadsidePartner>>> Handle(
        GetRecommendedRoadsidePartnersQuery query, CancellationToken cancellationToken)
    {
        var request = await _requestRepository.GetByIdAsync(query.RequestId, cancellationToken);

        if (request is null)
        {
            return Result<IReadOnlyCollection<RecommendedRoadsidePartner>>.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (request.RequesterUserId != query.RequestingUserId)
        {
            return Result<IReadOnlyCollection<RecommendedRoadsidePartner>>.Failure(ForbiddenError, ErrorType.Forbidden);
        }

        var vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId, cancellationToken);

        if (vehicle is null)
        {
            return Result<IReadOnlyCollection<RecommendedRoadsidePartner>>.Failure(VehicleNotFoundError, ErrorType.NotFound);
        }

        var candidates = await _partnerProfileRepository.GetCompatibleCandidatesAsync(
            request.ServiceType, vehicle.VehicleCategory, request.City, cancellationToken);

        var requestLocation = GeoCoordinate.Create(request.Latitude, request.Longitude);

        var recommendations = candidates
            .Select(candidate =>
            {
                double? distanceKm = null;

                if (candidate.Latitude is { } latitude && candidate.Longitude is { } longitude)
                {
                    distanceKm = (double)_distanceCalculator.CalculateKilometers(requestLocation, GeoCoordinate.Create(latitude, longitude));
                }

                return new RecommendedRoadsidePartner(candidate.UserId, candidate.BusinessName, candidate.City, distanceKm);
            })
            .OrderBy(candidate => candidate.DistanceKm ?? double.MaxValue)
            .ToList();

        return Result<IReadOnlyCollection<RecommendedRoadsidePartner>>.Success(recommendations);
    }
}
