using CarePulse.Api.Services.Common;

namespace CarePulse.Api.Services.Patients;

/// <summary>
/// Turns the From / To filters of the audit log into UTC boundaries PostgreSQL accepts.
///
/// The web page sends plain dates (for example 2026-10-14). ASP.NET reads those as a time with no
/// time zone ("Kind = Unspecified"), and PostgreSQL's driver rejects such a value for a
/// "timestamp with time zone" column, which used to make the audit log fail with a server error.
/// A date with no time of day means a whole clinic day (Sri Lanka time, as everywhere else in the app):
/// From starts at the beginning of that day and To runs to the end of that day. A value that does
/// carry a time or time zone is used exactly as given, converted to UTC.
/// </summary>
public static class AuditDateRange
{
    /// <returns>An inclusive lower bound and an EXCLUSIVE upper bound, both in UTC (or null when not filtered).</returns>
    public static (DateTime? FromUtc, DateTime? ToExclusiveUtc) Normalize(DateTime? from, DateTime? to) =>
        (from.HasValue ? Start(from.Value) : null, to.HasValue ? EndExclusive(to.Value) : null);

    private static bool IsWholeDay(DateTime value) => value.Kind == DateTimeKind.Unspecified && value.TimeOfDay == TimeSpan.Zero;

    private static DateTime Start(DateTime value)
    {
        if (IsWholeDay(value)) return ClinicTime.ToUtc(DateOnly.FromDateTime(value), TimeSpan.Zero);
        return AsUtc(value);
    }

    private static DateTime EndExclusive(DateTime value)
    {
        if (IsWholeDay(value)) return ClinicTime.ToUtc(DateOnly.FromDateTime(value).AddDays(1), TimeSpan.Zero);
        return AsUtc(value).AddTicks(1); // the original filter was "<= to", so keep the exact instant included
    }

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc), // a time with no zone is taken as UTC
    };
}
