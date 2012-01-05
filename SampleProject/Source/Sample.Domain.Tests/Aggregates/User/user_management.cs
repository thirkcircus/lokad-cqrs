#region Copyright (c) 2006-2011 LOKAD SAS. All rights reserved

// You must not remove this notice, or any other, from this software.
// This document is the property of LOKAD SAS and must not be disclosed

#endregion

using System;
using NUnit.Framework;
using Sample.Tests;

// ReSharper disable InconsistentNaming

namespace Sample.Aggregates.User
{
    [TestFixture]
    public sealed class user_management : ContainsSpecifications
    {
        static readonly UserId user = new UserId(1);
        static readonly SecurityId sec = new SecurityId(1);
        static readonly TimeSpan fiveMins = TimeSpan.FromMinutes(5);


        public Specification five_failures_within_threshold_should_lock = new UserSpec
            {
                Given =
                    {
                        new UserCreated(user, sec, fiveMins),
                        Failure(01),
                        Failure(02),
                        Failure(03),
                        Failure(03),
                    },
                When = new ReportUserLoginFailure(user, Time(1, 04), "local"),
                Expect =
                    {
                        Failure(04),
                        new UserLocked(user, "Login failed too many times", sec)
                    }
            };

        public Specification five_failures_outside_threshold_do_nothing = new UserSpec
            {
                Given =
                    {
                        new UserCreated(user, sec, fiveMins),
                        Failure(01),
                        Failure(02),
                        Failure(03),
                        Failure(03),
                    },
                When = new ReportUserLoginFailure(user, Time(1, 07), "local"),
                Expect =
                    {
                        Failure(07), 
                    }
            };

        public Specification successful_login_resets_failure_window = new UserSpec
            {
                Given =
                    {
                        new UserCreated(user, sec, fiveMins),
                        Failure(1),
                        Failure(2),
                        Failure(3),
                        Failure(4),
                        Success(4)
                    },
                When = new ReportUserLoginFailure(user, Time(1, 07), "local"),
                Expect =
                    {
                        Failure(7)
                    }
            };

        static IEvent<UserId> Failure(int minutes)
        {
            return new UserLoginFailureReported(user,Time(1, minutes), sec, "local");
        } 
        static IEvent<UserId> Success(int minutes)
        {
            return new UserLoginSuccessReported(user, Time(1, minutes), sec, "local");
        } 

     
        public Specification lock_user = new UserSpec
            {
                Given = {new UserCreated(user, sec, fiveMins)},
                When = new LockUser(user, "reason"),
                Expect = {new UserLocked(user, "reason", sec)},
            };

        public Specification unlock_user = new UserSpec
            {
                Given =
                    {
                        new UserCreated(user, sec, fiveMins),
                        new UserLocked(user, "locked", sec)
                    },
                When = new UnlockUser(user, "reason"),
                Expect = {new UserUnlocked(user, "reason", sec)},
            };


        public Specification ignore_dual_unlocks = new UserSpec
            {
                Given = {new UserCreated(user, sec, fiveMins)},
                When = new UnlockUser(user, "reason"),
            };

        public Specification ignore_dual_locks = new UserSpec
            {
                Given =
                    {
                        new UserCreated(user, sec, fiveMins),
                        new UserLocked(user, "locked", sec)
                    },
                When = new LockUser(user, "lock again")
            };

        public Specification no_premature_actions_on_nonexistent_aggregate = new UserFail()
            {
                When =new LockUser(user, "Reason"),
                Expect = { error => error.Name =="premature" }
            };

        public Specification no_zombies_allowed = new UserFail
            {
                Given =
                    {
                        new UserCreated(user, sec, TimeSpan.FromMinutes(5)),
                        new UserDeleted(user, sec)
                    },
                When = new LockUser(user, "sec"),
                Expect = {error => error.Name == "zombie" }

            };

        public Specification no_rebirth_in_this_realm = new UserFail
            {
                Given =
                    {
                        new UserCreated(user, sec, TimeSpan.FromMinutes(5))
                    },
                When = new CreateUser(user, sec),
                Expect = {error => error.Name == "rebirth"}
            };


        static DateTime Time(int hour, int minute = 0, int second = 0)
        {
            return new DateTime(2011, 1, 1, hour, minute, second, DateTimeKind.Unspecified);
        }
    }
}