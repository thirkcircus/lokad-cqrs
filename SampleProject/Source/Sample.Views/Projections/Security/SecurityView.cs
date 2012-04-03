#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2012 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD License
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;
using System.Collections.Generic;

namespace Sample.Security
{
    public sealed class SecurityView
    {
        public Dictionary<long, SecurityItem> Items { get; set; }

        public SecurityView()
        {
            Items = new Dictionary<long, SecurityItem>();
        }

        public void TryRemove(UserId userId)
        {
            Items.Remove(userId.Id);
        }

        public void AddItem(UserId userId, SecurityItemType type, string displayName, string value)
        {
            Items[userId.Id] = new SecurityItem
                {
                    Display = displayName,
                    Type = type,
                    Value = value,
                    UserId = userId.Id
                };
        }

        public void Lock(UserId id, string lockReason)
        {
            var item = Items[id.Id];
            item.Locked = true;
            item.LockReason = lockReason;
        }

        public void UpdateLogin(UserId id, DateTime timeUtc)
        {
            var item = Items[id.Id];
            if (item.LastLoginUtc < timeUtc)
            {
                item.LastLoginUtc = timeUtc;
            }
        }

        public void Unlock(UserId id)
        {
            var item = Items[id.Id];
            item.Locked = false;
            item.LockReason = null;
        }

        public void RenameDisplay(SecurityId id, string displayName)
        {
            Items[id.Id].Display = displayName;
        }
    }
}