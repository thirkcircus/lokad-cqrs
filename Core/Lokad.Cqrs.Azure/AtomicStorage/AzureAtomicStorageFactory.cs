#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;
using System.Collections.Generic;
using Lokad.Cqrs.AtomicStorage;

namespace Lokad.Cqrs.Feature.AtomicStorage
{
    public sealed class AzureAtomicStorageFactory : IAtomicStorageFactory
    {
        public IAtomicWriter<TKey, TEntity> GetEntityWriter<TKey, TEntity>()
        {
            var writer = new AzureAtomicWriter<TKey, TEntity>(_storage, _strategy);

            var value = Tuple.Create(typeof(TKey), typeof(TEntity));
            if (_initialized.Add(value))
            {
                // we've added a new record. Need to initialize
                writer.InitializeIfNeeded();
            }
            return writer;
        }

        public IAtomicReader<TKey, TEntity> GetEntityReader<TKey, TEntity>()
        {
            return new AzureAtomicReader<TKey, TEntity>(_storage, _strategy);
        }


        readonly IAtomicStorageStrategy _strategy;
        readonly IAzureStorageConfig _storage;

        readonly HashSet<Tuple<Type, Type>> _initialized = new HashSet<Tuple<Type, Type>>();

        public AzureAtomicStorageFactory(IAtomicStorageStrategy strategy, IAzureStorageConfig storage)
        {
            _strategy = strategy;
            _storage = storage;
        }
    }
}