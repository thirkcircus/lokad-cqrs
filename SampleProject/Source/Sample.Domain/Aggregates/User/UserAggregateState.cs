#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2012 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD License
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sample.Aggregates.Login
{
    public sealed class UserAggregateState : IAggregateState, IUserAggregateState
    {
        public SecurityId SecurityId { get; private set; }
        public string LockoutMessage { get; private set; }

        public int FailuresAllowed { get; private set; }
        public TimeSpan FailureLockoutWindow { get; private set; }
        public TimeSpan LoginActivityTrackingThreshold { get; private set; }
        public DateTime LastLoginUtc { get; private set; }
        public List<DateTime> TrackedLoginFailures { get; private set; }

        public bool Locked { get; private set; }

        public UserAggregateState(IEnumerable<IEvent<IIdentity>> events)
        {
            TrackedLoginFailures = new List<DateTime>();
            FailuresAllowed = 5;
            FailureLockoutWindow = TimeSpan.FromMinutes(5);
            // track every login by default
            LoginActivityTrackingThreshold = TimeSpan.FromMinutes(0);

            foreach (var e in events)
            {
                Apply(e);
            }
        }


        public bool DoesLastFailureWarrantLockout()
        {
            if (TrackedLoginFailures.Count < FailuresAllowed)
                return false;
            if ((TrackedLoginFailures.Last() - TrackedLoginFailures.First()) < FailureLockoutWindow)
                return true;
            return false;
        }

        public void When(UserLoginSuccessReported e)
        {
            TrackedLoginFailures.Clear();
            LastLoginUtc = e.TimeUtc;
        }

        public void When(UserLoginFailureReported e)
        {
            TrackedLoginFailures.Add(e.TimeUtc);
            // we track only X last failures
            while (TrackedLoginFailures.Count > FailuresAllowed)
            {
                TrackedLoginFailures.RemoveAt(0);
            }
        }

        public void When(UserLocked e)
        {
            LockoutMessage = e.LockReason;
            Locked = true;
        }

        public void When(UserUnlocked e)
        {
            Locked = false;
        }

        public void When(UserDeleted c)
        {
            Version = -1;
        }

        public void When(UserCreated e)
        {
            SecurityId = e.SecurityId;
        }

        public int Version { get; private set; }

        public void Apply(IEvent<IIdentity> e)
        {
            Version += 1;
            RedirectToWhen.InvokeEventOptional(this, e);
        }
    }
}