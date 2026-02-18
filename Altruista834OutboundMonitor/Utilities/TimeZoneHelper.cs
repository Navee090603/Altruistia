using System;

namespace Altruista834OutboundMonitor.Utilities
{
    public static class TimeZoneHelper
    {
        public static TimeZoneInfo Resolve(string configuredId)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(configuredId);
            }
            catch
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
            }
        }

        public static DateTime UtcToIst(DateTime utc, TimeZoneInfo tz) => TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
        public static DateTime IstNow(TimeZoneInfo tz) => TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);
    }
}
