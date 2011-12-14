using System.Collections.Concurrent;
using System.Linq;
using Lokad.Cqrs.Core.Outbox;

namespace Lokad.Cqrs.Feature.MemoryPartition
{
    public sealed class MemoryAccount : HideObjectMembersFromIntelliSense
    {
        readonly ConcurrentDictionary<string, BlockingCollection<byte[]>> _delivery =
            new ConcurrentDictionary<string, BlockingCollection<byte[]>>();

        public readonly ConcurrentDictionary<string, byte[]> Data = new ConcurrentDictionary<string, byte[]>();
        
        public MemoryPartitionInbox CreateInbox(params string[] queueNames)
        {
            var queues = queueNames
                .Select(n => _delivery.GetOrAdd(n, s => new BlockingCollection<byte[]>()))
                .ToArray();

            return new MemoryPartitionInbox(queues, queueNames);
        }

        public IQueueWriter CreateWriteQueue(string queueName)
        {
            return
                new MemoryQueueWriter(_delivery.GetOrAdd(queueName, s => new BlockingCollection<byte[]>()), queueName);
        }

          
    }
}