#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2012 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD License
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using Lokad.Cqrs.AtomicStorage;

namespace Sample
{
    public sealed class IdentityGenerator : IIdentityGenerator
    {
        readonly NuclearStorage _storage;

        public IdentityGenerator(NuclearStorage storage)
        {
            _storage = storage;
        }

        public long GetId()
        {
            var ix = new long[1];
            _storage.UpdateSingletonEnforcingNew<SampleEntityVector>(t => t.Reserve(ix));
            return ix[0];
        }
    }
}