using System;
using System.Collections.Generic;

namespace Lokad.Cqrs.AtomicStorage
{
    public interface IAtomicContainer 
    {
        IAtomicWriter<TKey,TEntity> GetEntityWriter<TKey,TEntity>();
        IAtomicReader<TKey,TEntity> GetEntityReader<TKey,TEntity>();
        IAtomicStorageStrategy Strategy { get; }
        IEnumerable<AtomicRecord> EnumerateContents();
        void WriteContents(IEnumerable<AtomicRecord> records);
        void Reset();
    }



    public sealed class AtomicRecord
    {
        /// <summary>
        /// Path of the view in the subfolder, using '/' as split on all platforms
        /// </summary>
        public readonly string Path;
        public readonly Func<byte[]> Read;

        public AtomicRecord(string path, Func<byte[]> read)
        {
            Path = path;
            Read = read;
        }
    }

}