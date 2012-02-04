using System;
using System.Collections.Concurrent;
using System.IO;

namespace Lokad.Cqrs.AtomicStorage
{
    public sealed class MemoryAtomicReaderWriter<TKey,TEntity> : IAtomicReader<TKey,TEntity>, IAtomicWriter<TKey,TEntity>
    {
        readonly IAtomicStorageStrategy _strategy;
        readonly string _folder;
        readonly ConcurrentDictionary<string, byte[]> _store;
        readonly string _root;

        public override string ToString()
        {
            return "memory://" + _store.GetHashCode() + "/" + _folder;
        }

        public MemoryAtomicReaderWriter(ConcurrentDictionary<string, byte[]> store, IAtomicStorageStrategy strategy, string optionalSubfolder)
        {
            _store = store;
            _strategy = strategy;

            _folder =  _strategy.GetFolderForEntity(typeof(TEntity),typeof(TKey));
            if (!string.IsNullOrEmpty(optionalSubfolder))
            {
                _folder = optionalSubfolder + '/' + _folder;
            }
        }

        string GetName(TKey key)
        {
            var name = _strategy.GetNameForEntity(typeof (TEntity), key);
            return _folder[_folder.Length - 1] == '/' ? _folder + name : _folder + '/' + name;
        }

        public bool TryGet(TKey key, out TEntity entity)
        {
            var name = GetName(key);
            byte[] bytes;
            if(_store.TryGetValue(name, out bytes))
            {
               using (var mem = new MemoryStream(bytes))
               {
                   entity = _strategy.Deserialize<TEntity>(mem);
                   return true;
               }
            }
            entity = default(TEntity);
            return false;
        }


        public TEntity AddOrUpdate(TKey key, Func<TEntity> addFactory, Func<TEntity, TEntity> update, AddOrUpdateHint hint)
        {
            var result = default(TEntity);
            _store.AddOrUpdate(GetName(key), s =>
                {
                    result = addFactory();
                    using (var memory = new MemoryStream())
                    {
                        _strategy.Serialize(result, memory);
                        return memory.ToArray();
                    }
                }, (s2, bytes) =>
                    {
                        TEntity entity;
                        using (var memory = new MemoryStream(bytes))
                        {
                            entity = _strategy.Deserialize<TEntity>(memory);
                        }
                        result = update(entity);
                        using (var memory = new MemoryStream())
                        {
                            _strategy.Serialize(result, memory);
                            return memory.ToArray();
                        }
                    });
            return result;
        }
     

        public bool TryDelete(TKey key)
        {
            byte[] bytes;
            return _store.TryRemove(GetName(key), out bytes);
        }
    }
}