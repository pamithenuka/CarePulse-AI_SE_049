using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CarePulse.Api.Services.Dispatch;

public class GoogleMapsService : IGoogleMapsService
{
    private readonly ILogger<GoogleMapsService> _logger;
    private const double EarthRadiusKm = 6371.0;
    private const double AverageCitySpeedKmh = 40.0;

    public GoogleMapsService(ILogger<GoogleMapsService> logger)
    {
        _logger = logger;
    }

    public async Task<(double DistanceKm, double EtaMinutes)> GetRouteMetricsAsync(double startLat, double startLng, double endLat, double endLng)
    {
        try
        {
            // Simulate API Call Delay
            await Task.Delay(100);
            
            // In a real scenario, we would call the Google Maps Directions API here.
            // For now, we immediately fallback to Haversine for reliability as per requirements.
            _logger.LogInformation("Using Haversine fallback for route metrics.");
            return CalculateHaversineMetrics(startLat, startLng, endLat, endLng);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Google Maps API call failed. Falling back to Haversine.");
            return CalculateHaversineMetrics(startLat, startLng, endLat, endLng);
        }
    }

    private (double DistanceKm, double EtaMinutes) CalculateHaversineMetrics(double startLat, double startLng, double endLat, double endLng)
    {
        var dLat = ToRadians(endLat - startLat);
        var dLng = ToRadians(endLng - startLng);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(startLat)) * Math.Cos(ToRadians(endLat)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var distanceKm = EarthRadiusKm * c;

        // Apply a multiplier for city routing (e.g. Manhattan distance roughly 1.4x straight line)
        var estimatedRouteDistanceKm = distanceKm * 1.4;
        
        var etaMinutes = (estimatedRouteDistanceKm / AverageCitySpeedKmh) * 60;

        return (estimatedRouteDistanceKm, etaMinutes);
    }

    private static double ToRadians(double angleIn10thofaDegree)
    {
        return angleIn10thofaDegree * (Math.PI / 180);
    }
}
