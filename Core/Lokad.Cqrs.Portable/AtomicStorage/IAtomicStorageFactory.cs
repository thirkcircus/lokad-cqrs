using System;
using System.Collections.Generic;

namespace Lokad.Cqrs.AtomicStorage
{
    public interface IAtomicStorageFactory 
    {
        IAtomicWriter<TKey,TEntity> GetEntityWriter<TKey,TEntity>();
        IAtomicReader<TKey,TEntity> GetEntityReader<TKey,TEntity>();

        IEnumerable<AtomicRecord> EnumerateContents();
        void WriteContents(IEnumerable<AtomicRecord> records);
        void Reset();
    }



    public sealed class AtomicRecord
    {
        public readonly string Path;
        public readonly Func<byte[]> Read;

        public AtomicRecord(string path, Func<byte[]> read)
        {
            Path = path;
            Read = read;
        }
    }

}