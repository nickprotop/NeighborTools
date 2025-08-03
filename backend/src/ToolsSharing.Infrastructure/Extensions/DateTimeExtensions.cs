using System;

namespace ToolsSharing.Infrastructure.Extensions;

/// <summary>
/// PostgreSQL-compatible DateTime extension methods
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Ensures DateTime is UTC-compatible for PostgreSQL
    /// Converts Unspecified DateTimes to UTC, leaves UTC unchanged
    /// </summary>
    /// <param name="dateTime">The DateTime to ensure is UTC</param>
    /// <returns>UTC DateTime suitable for PostgreSQL</returns>
    public static DateTime EnsureUtc(this DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
            _ => dateTime
        };
    }

    /// <summary>
    /// Creates a UTC DateTime from date components for PostgreSQL compatibility
    /// </summary>
    /// <param name="year">Year</param>
    /// <param name="month">Month</param>
    /// <param name="day">Day</param>
    /// <returns>UTC DateTime</returns>
    public static DateTime CreateUtcDate(int year, int month, int day)
    {
        return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
    }

    /// <summary>
    /// Creates a UTC DateTime from date and time components for PostgreSQL compatibility
    /// </summary>
    /// <param name="year">Year</param>
    /// <param name="month">Month</param>
    /// <param name="day">Day</param>
    /// <param name="hour">Hour</param>
    /// <param name="minute">Minute</param>
    /// <param name="second">Second</param>
    /// <returns>UTC DateTime</returns>
    public static DateTime CreateUtcDateTime(int year, int month, int day, int hour, int minute, int second)
    {
        return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
    }
}