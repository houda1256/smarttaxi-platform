using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.ValueObjects;

namespace SmartTaxi.Infrastructure.Rides.Services;

/// <summary>
/// Estimates route distance and duration using the Smart ETA prediction microservice (ML champion model),
/// with automatic graceful fallback to Haversine urban speed heuristics if the service is unreachable.
/// </summary>
internal sealed class RouteEstimationService : IRouteEstimationService
{
    private const decimal AssumedAverageSpeedKmh = 30m;
    private static readonly HttpClient SharedHttpClient = new() { Timeout = TimeSpan.FromMilliseconds(800) };

    private readonly IDistanceCalculator _distanceCalculator;
    private readonly HttpClient _httpClient;
    private readonly ILogger<RouteEstimationService>? _logger;
    private readonly string _etaBaseUrl;

    public RouteEstimationService(
        IDistanceCalculator distanceCalculator,
        IConfiguration? configuration = null,
        ILogger<RouteEstimationService>? logger = null,
        HttpClient? httpClient = null)
    {
        _distanceCalculator = distanceCalculator;
        _httpClient = httpClient ?? SharedHttpClient;
        _logger = logger;
        _etaBaseUrl = configuration?["EtaService:BaseUrl"] ?? "http://localhost:8000";
    }

    public RouteEstimate Estimate(GeoCoordinate from, GeoCoordinate to)
    {
        try
        {
            var payload = new
            {
                pickup_latitude = (double)from.Latitude,
                pickup_longitude = (double)from.Longitude,
                dropoff_latitude = (double)to.Latitude,
                dropoff_longitude = (double)to.Longitude,
                pickup_datetime = DateTime.UtcNow.ToString("o"),
                passenger_count = 1
            };

            var response = _httpClient.PostAsJsonAsync($"{_etaBaseUrl}/predict/trip-duration", payload)
                                      .GetAwaiter().GetResult();

            if (response.IsSuccessStatusCode)
            {
                var result = response.Content.ReadFromJsonAsync<EtaPredictionResult>()
                                             .GetAwaiter().GetResult();

                if (result != null && result.EtaMinutes > 0)
                {
                    return new RouteEstimate(
                        (decimal)Math.Round(result.DistanceKm, 2),
                        (int)Math.Max(1, Math.Ceiling(result.EtaMinutes))
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "ETA Prediction Service unreachable at {BaseUrl}. Falling back to urban speed heuristic.", _etaBaseUrl);
        }

        // Classical routing fallback calculation
        var distanceKm = _distanceCalculator.CalculateKilometers(from, to);
        var durationMinutes = (int)Math.Ceiling(distanceKm / AssumedAverageSpeedKmh * 60);

        return new RouteEstimate(Math.Round(distanceKm, 2), Math.Max(durationMinutes, 1));
    }

    private sealed record EtaPredictionResult(
        [property: JsonPropertyName("eta_seconds")] int EtaSeconds,
        [property: JsonPropertyName("eta_minutes")] double EtaMinutes,
        [property: JsonPropertyName("distance_km")] double DistanceKm,
        [property: JsonPropertyName("quality_flag")] string QualityFlag,
        [property: JsonPropertyName("fallback_used")] bool FallbackUsed
    );
}
