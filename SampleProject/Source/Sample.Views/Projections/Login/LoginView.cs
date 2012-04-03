#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2012 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD License
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;

namespace Sample.Login
{
    public sealed class LoginView
    {
        public string Display { get; set; }
        public SecurityId Security { get; set; }
        public string Token { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public string Key { get; set; }
        public bool IsLocked { get; set; }
        public string LockoutMessage { get; set; }
        public LoginViewType Type { get; set; }
        public string Identity { get; set; }
        public TimeSpan LoginTrackingThreshold { get; set; }
        public DateTime LastLoginUtc { get; set; }
    }
}