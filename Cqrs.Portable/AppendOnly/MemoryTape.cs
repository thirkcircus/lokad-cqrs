using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Lokad.Cqrs.TapeStorage
{
    public sealed class MemoryAppendOnlyStore : IAppendOnlyStore
    {
        ConcurrentDictionary<string, IList<DataWithVersion>> _dict = new ConcurrentDictionary<string, IList<DataWithVersion>>();
        IList<DataWithName> _all = new List<DataWithName>(); 
 

        public void InitializeForWriting()
        {
            
        }

        public void Append(string streamName, byte[] data, long expectedStreamVersion = -1)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (data.Length == 0)
                throw new ArgumentException("Buffer must contain at least one byte.");

            var result = _dict.AddOrUpdate(streamName, s =>
            {

                if (expectedStreamVersion >= 0)
                {
                    if (expectedStreamVersion != 0)
                        throw new AppendOnlyStoreConcurrencyException(expectedStreamVersion, 0, streamName);
                }
                var records = new List<DataWithVersion> { new DataWithVersion(1, data) };
                return records;
            }, (s, list) =>
            {
                var version = list.Count;
                if (expectedStreamVersion >= 0)
                {
                    if (expectedStreamVersion != version)
                        throw new AppendOnlyStoreConcurrencyException(expectedStreamVersion, version, streamName);
                }
                return list.Concat(new[] { new DataWithVersion(version + 1, data) }).ToList();
            });
            
            _all = new List<DataWithName>(_all) { new DataWithName(streamName, data, result.Count)};
        }

        public IEnumerable<DataWithVersion> ReadRecords(string streamName, long afterVersion, int maxCount)
        {
            if (afterVersion < 0)
                throw new ArgumentOutOfRangeException("afterVersion", "Must be zero or greater.");

            if (maxCount <= 0)
                throw new ArgumentOutOfRangeException("maxCount", "Must be more than zero.");

            IList<DataWithVersion> bytes;
            if (_dict.TryGetValue(streamName, out bytes))
            {
                foreach (var bytese in bytes.Where(r => r.Version > afterVersion).Take(maxCount))
                {
                    yield return bytese;
                }
            }
        }

        public IEnumerable<DataWithName> ReadRecords(long afterVersion, int maxCount)
        {
            return _all.Skip((int) afterVersion).Take(maxCount);
        }

        public void Close()
        {
            
        }

        public void Dispose()
        {
            
        }
    }
}