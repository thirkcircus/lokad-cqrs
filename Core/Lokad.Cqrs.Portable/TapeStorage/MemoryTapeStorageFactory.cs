using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Lokad.Cqrs.TapeStorage
{
    public sealed class MemoryTapeStorageFactory : ITapeStorageFactory
    {
        readonly ConcurrentDictionary<string, List<byte[]>> _storage;
        readonly string _prefix;

        public MemoryTapeStorageFactory(ConcurrentDictionary<string, List<byte[]>> storage, string prefix)
        {
            _storage = storage;
            _prefix = prefix;
        }

        public void InitializeForWriting()
        {
        }
        public ITapeStream GetOrCreateStream(string name)
        {
            return new MemoryTapeStream(_storage, _prefix + ":" + name);
        }
    }
}