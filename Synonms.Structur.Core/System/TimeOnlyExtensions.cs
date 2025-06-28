namespace Synonms.Structur.Core.System;

public static class TimeOnlyExtensions
{
    public static TimeOnly Now =>
        TimeOnly.FromDateTime(DateTime.Now);
    
    public static TimeOnly NowUtc =>
        TimeOnly.FromDateTime(DateTime.UtcNow);

    public static DateTime ToDateTime(this TimeOnly time, DateOnly date) =>
        date.ToDateTime(time);
}