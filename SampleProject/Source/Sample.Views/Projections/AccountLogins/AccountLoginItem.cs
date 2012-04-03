#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2012 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD License
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;

namespace Sample.AccountLogins
{
    public sealed class AccountLoginItem
    {
        public string Display { get; set; }
        public UserId UserId { get; set; }
        public bool IsLocked { get; set; }
        public string Value { get; set; }
        public string Type { get; set; }
        public DateTime LastLoginUtc { get; set; }
    }
}