using System;

namespace CoKhiThaCo.Helpers
{
    public static class TimeHelper
    {
        private static readonly TimeZoneInfo VnTimeZone =
            FindTimeZoneById("Asia/Ho_Chi_Minh", "SE Asia Standard Time");

        public static DateTime Now =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VnTimeZone);

        public static DateTime Today => Now.Date;

        public static DateTime UtcNow => DateTime.UtcNow;

        public static TimeZoneInfo FindTimeZoneById(params string[] ids)
        {
            foreach (var id in ids)
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }
            throw new TimeZoneNotFoundException($"Khong tim thay timezone: {string.Join(", ", ids)}");
        }
    }
}
