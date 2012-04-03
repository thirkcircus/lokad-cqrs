using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Cqrs.TapeStorage
{
    public sealed class TapeStorageInitilization : IEngineProcess
    {
        readonly IEnumerable<ITapeContainer> _storage;
        public TapeStorageInitilization(IEnumerable<ITapeContainer> storage)
        {
            _storage = storage;
        }

        public void Dispose()
        {
            
        }

        public void Initialize()
        {
            foreach (var factory in _storage)
            {
                factory.InitializeForWriting();
            }
        }

        public Task Start(CancellationToken token)
        {
            // don't do anything
            return new Task(() => { });
        }
    }
}