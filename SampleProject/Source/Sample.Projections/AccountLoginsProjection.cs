#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2012 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD License
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using Lokad.Cqrs.AtomicStorage;
using Sample.AccountLogins;

namespace Sample.Projections
{
    sealed class AccountLoginsProjection
    {
        readonly IAtomicWriter<SecurityId, AccountLoginsView> _writer;

        public AccountLoginsProjection(IAtomicWriter<SecurityId, AccountLoginsView> writer)
        {
            _writer = writer;
        }


        public void When(SecurityPasswordAdded e)
        {
            _writer.UpdateOrThrow(e.Id, v => v.AddItem(e.UserId, e.DisplayName, e.Login, "Password"));
        }

        public void When(SecurityIdentityAdded e)
        {
            var describe = e.Identity;

            if (describe.Contains("google.com"))
            {
                describe = "Google Identity";
            }
            _writer.UpdateOrThrow(e.Id, v => v.AddItem(e.UserId, e.DisplayName, describe, "Identity"));
        }

        public void When(SecurityKeyAdded e)
        {
            var describe = e.Key;
            _writer.UpdateOrThrow(e.Id, v => v.AddItem(e.UserId, e.DisplayName, describe, "Key"));
        }

        public void When(UserLocked e)
        {
            _writer.UpdateOrThrow(e.SecurityId, cv => cv.Update(e.Id, v => v.IsLocked = true));
        }

        public void When(UserLoginSuccessReported e)
        {
            _writer.UpdateOrThrow(e.SecurityId, cv => cv.Update(e.Id, v => v.LastLoginUtc = e.TimeUtc));
        }

        public void When(SecurityAggregateCreated e)
        {
            _writer.UpdateEnforcingNew(e.Id, view => { }, AddOrUpdateHint.ProbablyDoesNotExist);
        }

        public void When(SecurityItemDisplayNameUpdated e)
        {
            _writer.UpdateOrThrow(e.Id, cv => cv.Update(e.UserId, v => v.Display = e.DisplayName));
        }

        public void When(SecurityItemRemoved e)
        {
            _writer.UpdateOrThrow(e.Id, cv => cv.Remove(e.UserId));
        }
    }
}