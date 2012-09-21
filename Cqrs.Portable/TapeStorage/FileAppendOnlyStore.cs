using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Lokad.Cqrs.TapeStorage
{
    /// <summary>
    /// Simple embedded append-only store that uses Riak.Bitcask model
    /// for keeping records
    /// </summary>
    public class FileAppendOnlyStore : IAppendOnlyStore
    {
        readonly DirectoryInfo _info;
        
        // used to synchronize access between threads within a process
        
        readonly ReaderWriterLockSlim _thread = new ReaderWriterLockSlim();
        // used to prevent writer access to store to other processes
        FileStream _lock;
        FileStream _currentWriter;

        // caches
        readonly ConcurrentDictionary<string, DataWithVersion[]> _cacheByKey = new ConcurrentDictionary<string, DataWithVersion[]>();
        DataWithKey[] _cacheFull = new DataWithKey[0];

        public void Initialize()
        {
            if (!_info.Exists)
                _info.Create();
            // grab the ownership
            _lock = new FileStream(Path.Combine(_info.FullName, "lock"), 
                FileMode.OpenOrCreate, 
                FileAccess.ReadWrite, 
                FileShare.None, 
                8,
                FileOptions.DeleteOnClose);

            LoadCaches();
        }

        public void LoadCaches()
        {
            try
            {
                _thread.EnterWriteLock();
                _cacheFull = new DataWithKey[0];
                foreach (var record in EnumerateHistory())
                {
                    AddToCaches(record.Name, record.Bytes, record.Stamp);
                }
                
            }
            finally
            {
                _thread.ExitWriteLock();
            }
        }

        IEnumerable<StorageFrameDecoded> EnumerateHistory()
        {
            // cleanup old pending files
            // load indexes
            // build and save missing indexes
            var datFiles = _info.EnumerateFiles("*.dat");

            foreach (var fileInfo in datFiles.OrderBy(fi => fi.Name))
            {
                // quick cleanup
                if (fileInfo.Length == 0)
                {
                    fileInfo.Delete();
                }

                using (var reader = fileInfo.OpenRead())
                {
                    StorageFrameDecoded result;
                    while (StorageFramesEvil.TryReadFrame(reader,out result))
                    {
                        yield return result;
                    }
                }
            }
        }


        public void Dispose()
        {
            if (!_closed)
                Close();
        }

        public FileAppendOnlyStore(DirectoryInfo info)
        {
            _info = info;
        }

        public void Append(string streamName, byte[] data, long expectedStreamVersion = -1)
        {
            // should be locked
            try
            {
                _thread.EnterWriteLock();

                var list = _cacheByKey.GetOrAdd(streamName, s => new DataWithVersion[0]);
                if (expectedStreamVersion >= 0)
                {
                    if (list.Length != expectedStreamVersion)
                        throw new AppendOnlyStoreConcurrencyException(expectedStreamVersion, list.Length, streamName);
                }

                EnsureWriterExists(_cacheFull.Length);
                long commit = list.Length + 1;

                PersistInFile(streamName, data, commit);
                AddToCaches(streamName, data, commit);
            }
            catch
            {
                Close();
            }
            finally
            {
                _thread.ExitWriteLock();
            }
        }

        void PersistInFile(string key, byte[] buffer, long commit)
        {
            StorageFramesEvil.WriteFrame(key, commit, buffer, _currentWriter);
            // make sure that we persist
            // NB: this is not guaranteed to work on Linux
            _currentWriter.Flush(true);
        }

        void EnsureWriterExists(long version)
        {
            if (_currentWriter != null) return;

            var fileName = string.Format("{0:00000000}-{1:yyyy-MM-dd-HHmmss}.dat", version, DateTime.UtcNow);
            _currentWriter = File.OpenWrite(Path.Combine(_info.FullName, fileName));
        }

        void AddToCaches(string key, byte[] buffer, long commit)
        {
            var storeVersion = _cacheFull.Length + 1;
            var record = new DataWithVersion(commit, buffer, storeVersion);
            _cacheFull = ImmutableAdd(_cacheFull, new DataWithKey(key, buffer, commit, storeVersion));
            _cacheByKey.AddOrUpdate(key, s => new[] {record}, (s, records) => ImmutableAdd(records, record));
        }

        static T[] ImmutableAdd<T>(T[] source, T item)
        {
            var copy = new T[source.Length + 1];
            Array.Copy(source, copy, source.Length);
            copy[source.Length] = item;
            return copy;
        } 

        public IEnumerable<DataWithVersion> ReadRecords(string streamName, long afterVersion, int maxCount)
        {
            // no lock is needed.
            DataWithVersion[] list;
            return _cacheByKey.TryGetValue(streamName, out list) ? list : Enumerable.Empty<DataWithVersion>();
        }

        public IEnumerable<DataWithKey> ReadRecords(long afterVersion, int maxCount)
        {
            // collection is immutable so we don't care about locks
            return _cacheFull.Skip((int) afterVersion).Take(maxCount);
        }

        bool _closed;

        public void Close()
        {
            using (_lock)
            using (_currentWriter)
            {
                _closed = true;
            }
        }

        
        public long GetCurrentVersion()
        {
            return _cacheFull.Length;
        }
    }
}