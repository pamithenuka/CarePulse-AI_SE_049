namespace CarePulse.Api.Services.Common;

public static class ClinicTime
{
    // Rosters are clinic wall time; database timestamps are always UTC.
    public static DateTime ToUtc(DateOnly date, TimeSpan time) => TimeZoneInfo.ConvertTimeToUtc(
        DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue).Add(time), DateTimeKind.Unspecified),
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo"));
}
