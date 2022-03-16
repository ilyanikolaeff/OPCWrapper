using System;

namespace OPCWrapper
{
    internal static class DateTimeExtensions
    {
        internal static DateTime Truncate(this DateTime dateTime, long resolution)
        {
            return new DateTime(dateTime.Ticks - (dateTime.Ticks % resolution), dateTime.Kind);
        }
    }
}
