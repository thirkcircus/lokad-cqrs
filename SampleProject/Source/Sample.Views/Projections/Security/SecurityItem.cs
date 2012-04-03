#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2012 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD License
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;

namespace Sample.Security
{
    public sealed class SecurityItem
    {
        public long UserId { get; set; }
        public string Display { get; set; }
        public SecurityItemType Type { get; set; }
        public string Value { get; set; }
        public bool Locked { get; set; }
        public string LockReason { get; set; }
        public DateTime LastLoginUtc { get; set; }
    }
}