#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2012 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD License
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;

namespace Sample
{
    public static class Current
    {
        static Func<DateTime> _getTime = GetUtc;

        static DateTime GetUtc()
        {
            return new DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Unspecified);
        }

        public static void DateIs(DateTime time)
        {
            _getTime = () => new DateTime(time.Ticks, DateTimeKind.Unspecified);
        }

        public static void DateIs(int year, int month = 1, int day = 1)
        {
            DateIs(new DateTime(year, month, day));
        }


        public static void Reset()
        {
            _getTime = GetUtc;
        }

        public static DateTime UtcNow
        {
            get { return _getTime(); }
        }
    }
}