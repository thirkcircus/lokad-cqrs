#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2012 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD License
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System.Collections.Generic;

namespace Sample.LoginsIndex
{
    public sealed class LoginsIndexView
    {
        public IDictionary<string, long> Logins { get; private set; }
        public IDictionary<string, long> Keys { get; private set; }
        public IDictionary<string, long> Identities { get; private set; }

        public LoginsIndexView()
        {
            Logins = new Dictionary<string, long>();
            Keys = new Dictionary<string, long>();
            Identities = new Dictionary<string, long>();
        }
    }
}