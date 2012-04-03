#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2012 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD License
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sample.AccountLogins
{
    public sealed class AccountLoginsView
    {
        public IList<AccountLoginItem> Items { get; private set; }

        public void AddItem(UserId id, string display, string describe, string type)
        {
            Items.Add(new AccountLoginItem
                {
                    Value = describe,
                    Display = display,
                    UserId = id,
                    Type = type
                });
        }

        public void Update(UserId id, Action<AccountLoginItem> update)
        {
            var item = Items.Where(i => i.UserId.Equals(id)).FirstOrDefault();
            if (null != item)
            {
                update(item);
            }
        }

        public void Remove(UserId id)
        {
            var old = Items.Where(i => i.UserId.Equals(id)).ToArray();
            foreach (var accountLoginItem in old)
            {
                Items.Remove(accountLoginItem);
            }
        }

        public AccountLoginsView()
        {
            Items = new List<AccountLoginItem>();
        }
    }
}