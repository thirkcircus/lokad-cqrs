#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Lokad.Cqrs.AtomicStorage
{
    public sealed class MemoryAtomicContainer : IAtomicContainer
    {
        ConcurrentDictionary<string, byte[]> _store;
        readonly IAtomicStorageStrategy _strategy;
        readonly string _optionalFolder;

        public MemoryAtomicContainer(ConcurrentDictionary<string,byte[]> store, IAtomicStorageStrategy strategy, string optionalFolder = null)
        {
            _store = store;
            _strategy = strategy;
            _optionalFolder = optionalFolder;
        }

        public IAtomicWriter<TKey,TEntity> GetEntityWriter<TKey,TEntity>()
        {
            return new MemoryAtomicReaderWriter<TKey, TEntity>(_store,_strategy, _optionalFolder);
        }


        public void WriteContents(IEnumerable<AtomicRecord> records)
        {
            
            var pairs = records.Select(r => new KeyValuePair<string, byte[]>(r.Path, r.Read())).ToArray();
            _store = new ConcurrentDictionary<string, byte[]>(pairs);
        }

        public void Reset()
        {
            _store.Clear();
        }


        public IAtomicReader<TKey, TEntity> GetEntityReader<TKey, TEntity>()
        {
            return new MemoryAtomicReaderWriter<TKey, TEntity>(_store,_strategy, _optionalFolder);
        }

        public IAtomicStorageStrategy Strategy
        {
            get { return _strategy; }
        }

        public IEnumerable<AtomicRecord> EnumerateContents()
        {
            // we normalize path to common symbol
            return _store.Select(s => new AtomicRecord(s.Key.Replace('\\', '/'), () => s.Value));
        }

        public IEnumerable<string> Initialize()
        {
            return Enumerable.Empty<string>();
        }
    }
}