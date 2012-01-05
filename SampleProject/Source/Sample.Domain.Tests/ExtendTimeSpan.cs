using System;

namespace Sample
{
    public static class ExtendTimeSpan
    {
        public static TimeSpan Days(this int days)
        {
            return TimeSpan.FromDays(days);
        }
    }
}