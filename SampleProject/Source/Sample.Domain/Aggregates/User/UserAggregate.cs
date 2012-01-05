#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2012 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD License
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;

namespace Sample.Aggregates.Login
{
    public sealed class UserAggregate : IAggregate<UserId>, IUserAggregate
    {
        readonly UserAggregateState _state;
        readonly Action<IEvent<UserId>> _observer;

        /// <summary>
        /// If relogin happens within the interval, we don't track it
        /// </summary>
        public static readonly TimeSpan DefaultLoginActivityThreshold = TimeSpan.FromMinutes(10);

        public UserAggregate(UserAggregateState state, Action<IEvent<UserId>> observer)
        {
            _state = state;
            _observer = observer;
        }

        public void Execute(ICommand<UserId> c)
        {
            ThrowOnInvalidStateTransition(c);

            RedirectToWhen.InvokeCommand(this, c);
        }

        public void When(CreateUser e)
        {
            if (_state.Version != 0)
                throw new DomainError("User already has non-zero version");

            Apply(new UserCreated(e.Id, e.SecurityId, DefaultLoginActivityThreshold));
        }

        public void When(ReportUserLoginFailure c)
        {
            Apply(new UserLoginFailureReported(c.Id, c.TimeUtc, _state.SecurityId, c.Ip));
            if (_state.DoesLastFailureWarrantLockout())
            {
                Apply(new UserLocked(c.Id, "Login failed too many times", _state.SecurityId));
            }
        }

        public void When(ReportUserLoginSuccess c)
        {
            Apply(new UserLoginSuccessReported(c.Id, c.TimeUtc, _state.SecurityId, c.Ip));
        }

        public void When(UnlockUser c)
        {
            if (false == _state.Locked)
                return;

            Apply(new UserUnlocked(c.Id, c.UnlockReason, _state.SecurityId));
        }

        public void When(DeleteUser c)
        {
            Apply(new UserDeleted(c.Id, _state.SecurityId));
        }

        public void When(LockUser c)
        {
            if (_state.Locked)
                return;

            Apply(new UserLocked(c.Id, c.LockReason, _state.SecurityId));
        }

        void ThrowOnInvalidStateTransition(ICommand<UserId> e)
        {
            if (_state.Version == 0)
            {
                if (e is CreateUser)
                {
                    return;
                }
                throw DomainError.Named("premature", "Can't do anything to unexistent aggregate");
            }
            if (_state.Version == -1)
            {
                throw DomainError.Named("zombie", "Can't do anything to deleted aggregate.");
            }
            if (e is CreateUser)
                throw DomainError.Named("rebirth", "Can't create aggregate that already exists");
        }

        void Apply(IEvent<UserId> e)
        {
            _state.Apply(e);
            _observer(e);
        }
    }
}