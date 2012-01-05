#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2012 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD License
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;
using Lokad.Cqrs.AtomicStorage;
using Sample.Login;

namespace Sample.Projections
{
    sealed class LoginViewProjection
    {
        readonly IAtomicWriter<UserId, LoginView> _writer;

        public LoginViewProjection(IAtomicWriter<UserId, LoginView> writer)
        {
            _writer = writer;
        }

        static TimeSpan DefaultThreshold = TimeSpan.FromMinutes(5);

        public void When(SecurityPasswordAdded e)
        {
            _writer.Add(e.UserId, new LoginView
                {
                    Security = e.Id,
                    Display = e.DisplayName,
                    Token = e.Token,
                    IsLocked = false,
                    PasswordHash = e.PasswordHash,
                    PasswordSalt = e.PasswordSalt,
                    Type = LoginViewType.Password,
                    LoginTrackingThreshold = DefaultThreshold
                });
        }

        public void When(SecurityIdentityAdded e)
        {
            _writer.Add(e.UserId, new LoginView
                {
                    Security = e.Id,
                    Display = e.DisplayName,
                    Token = e.Token,
                    Identity = e.Identity,
                    Type = LoginViewType.Identity,
                    LoginTrackingThreshold = DefaultThreshold
                });
        }

        public void When(SecurityKeyAdded e)
        {
            _writer.Add(e.UserId, new LoginView
                {
                    Security = e.Id,
                    Display = e.DisplayName,
                    Token = e.Token,
                    Key = e.Key,
                    Type = LoginViewType.Key,
                    LoginTrackingThreshold = DefaultThreshold
                });
        }

        public void When(UserLocked e)
        {
            _writer.UpdateOrThrow(e.Id, lv =>
                {
                    lv.IsLocked = true;
                    lv.LockoutMessage = e.LockReason;
                });
        }

        public void When(UserUnlocked e)
        {
            _writer.UpdateOrThrow(e.Id, lv =>
                {
                    lv.IsLocked = false;
                    lv.LockoutMessage = null;
                });
        }

        public void When(UserCreated e)
        {
            _writer.UpdateOrThrow(e.Id, lv => { lv.LoginTrackingThreshold = e.ActivityThreshold; });
        }

        public void When(UserLoginSuccessReported e)
        {
            _writer.UpdateOrThrow(e.Id, lv => { lv.LastLoginUtc = e.TimeUtc; });
        }

        public void When(SecurityItemDisplayNameUpdated e)
        {
            _writer.UpdateOrThrow(e.UserId, lv => lv.Display = e.DisplayName);
        }

        public void When(SecurityItemRemoved e)
        {
            _writer.TryDelete(e.UserId);
        }
    }
}