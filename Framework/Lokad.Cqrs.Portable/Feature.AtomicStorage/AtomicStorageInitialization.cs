#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Lokad.Cqrs.Build.Engine;

namespace Lokad.Cqrs.Feature.AtomicStorage
{
    public sealed class AtomicStorageInitialization : IEngineStartupTask
    {
        readonly IEnumerable<IAtomicStorageFactory> _storage;

        public AtomicStorageInitialization(IEnumerable<IAtomicStorageFactory> storage)
        {
            _storage = storage;
        }

        public void Execute(CqrsEngineHost host)
        {
            foreach (var atomicStorageFactory in _storage)
            {
                var folders = atomicStorageFactory.Initialize();
                if (folders.Any())
                {
                    SystemObserver.Notify(new AtomicStorageInitialized(folders.ToArray(), atomicStorageFactory.GetType()));
                }
            }
        }
    }
}