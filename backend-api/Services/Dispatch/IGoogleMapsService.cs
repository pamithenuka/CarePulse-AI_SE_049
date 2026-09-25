using System.Threading.Tasks;

namespace CarePulse.Api.Services.Dispatch;

public interface IGoogleMapsService
{
    Task<(double DistanceKm, double EtaMinutes)> GetRouteMetricsAsync(double startLat, double startLng, double endLat, double endLng);
}
