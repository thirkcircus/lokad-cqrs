#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2012 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD License
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using Lokad.Cqrs.AtomicStorage;
using Sample.Security;

namespace Sample.Projections
{
    sealed class SecurityProjection
    {
        readonly IAtomicWriter<SecurityId, SecurityView> _writer;

        public SecurityProjection(IAtomicWriter<SecurityId, SecurityView> writer)
        {
            _writer = writer;
        }

        public void When(SecurityAggregateCreated e)
        {
            _writer.Add(e.Id, new SecurityView());
        }

        public void When(SecurityKeyAdded e)
        {
            _writer.UpdateOrThrow(e.Id, view => view.AddItem(e.UserId, SecurityItemType.Key, e.DisplayName, e.Key));
        }

        public void When(SecurityIdentityAdded e)
        {
            _writer.UpdateOrThrow(e.Id,
                view => view.AddItem(e.UserId, SecurityItemType.Identity, e.DisplayName, e.Identity));
        }

        public void When(SecurityPasswordAdded e)
        {
            _writer.UpdateOrThrow(e.Id, view => view.AddItem(e.UserId, SecurityItemType.Pass, e.DisplayName, e.Login));
        }

        public void When(SecurityItemRemoved e)
        {
            _writer.UpdateOrThrow(e.Id, view => view.TryRemove(e.UserId));
        }

        public void When(UserLocked e)
        {
            _writer.UpdateOrThrow(e.SecurityId, view => view.Lock(e.Id, e.LockReason));
        }

        public void When(UserLoginSuccessReported e)
        {
            _writer.UpdateOrThrow(e.SecurityId, view => view.UpdateLogin(e.Id, e.TimeUtc));
        }

        public void When(UserUnlocked e)
        {
            _writer.UpdateOrThrow(e.SecurityId, view => view.Unlock(e.Id));
        }

        public void When(SecurityItemDisplayNameUpdated e)
        {
            _writer.UpdateOrThrow(e.Id, view => view.RenameDisplay(e.Id, e.DisplayName));
        }
    }
}