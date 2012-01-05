using System;
using System.Collections.Generic;

namespace Lokad.Cqrs.AtomicStorage
{
    public sealed class FileAtomicStorageFactory : IAtomicStorageFactory
    {
        readonly string _folderPath;
        readonly IAtomicStorageStrategy _strategy;


        public FileAtomicStorageFactory(string folderPath, IAtomicStorageStrategy strategy)
        {
            _folderPath = folderPath;
            _strategy = strategy;
        }


        readonly HashSet<Tuple<Type, Type>> _initialized = new HashSet<Tuple<Type, Type>>();


        public IAtomicWriter<TKey, TEntity> GetEntityWriter<TKey, TEntity>()
        {
            var container = new FileAtomicContainer<TKey, TEntity>(_folderPath, _strategy);
            if (_initialized.Add(Tuple.Create(typeof(TKey),typeof(TEntity))))
            {
                container.InitIfNeeded();
            }
            return container;
        }

        public IAtomicReader<TKey, TEntity> GetEntityReader<TKey, TEntity>()
        {
            return new FileAtomicContainer<TKey, TEntity>(_folderPath, _strategy);
        }
    }
}