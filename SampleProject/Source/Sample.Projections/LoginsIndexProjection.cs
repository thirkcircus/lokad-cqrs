#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2012 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD License
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using Lokad.Cqrs;
using Lokad.Cqrs.AtomicStorage;
using Sample.LoginsIndex;

namespace Sample.Projections
{
    public sealed class LoginsIndexProjection
    {
        readonly IAtomicWriter<unit, LoginsIndexView> _writer;

        public LoginsIndexProjection(IAtomicWriter<unit, LoginsIndexView> writer)
        {
            _writer = writer;
        }

        public void When(SecurityPasswordAdded e)
        {
            _writer.UpdateEnforcingNew(unit.it, si => si.Logins[e.Login] = e.UserId.Id);
        }

        public void When(SecurityIdentityAdded e)
        {
            _writer.UpdateEnforcingNew(unit.it, si => si.Identities[e.Identity] = e.UserId.Id);
        }

        public void When(SecurityKeyAdded e)
        {
            _writer.UpdateEnforcingNew(unit.it, si => si.Keys[e.Key] = e.UserId.Id);
        }

        public void When(SecurityItemRemoved e)
        {
            _writer.UpdateEnforcingNew(unit.it, si => si.Keys.Remove(e.Lookup));
        }
    }
}