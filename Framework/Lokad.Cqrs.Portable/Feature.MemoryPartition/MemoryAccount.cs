using System.Collections.Concurrent;
using System.Linq;
using Lokad.Cqrs.Core.Outbox;

namespace Lokad.Cqrs.Feature.MemoryPartition
{
    public sealed class MemoryAccount
    {
        readonly ConcurrentDictionary<string, BlockingCollection<byte[]>> _delivery =
            new ConcurrentDictionary<string, BlockingCollection<byte[]>>();

        public MemoryPartitionInbox GetMemoryInbox(string[] queueNames)
        {
            var queues = queueNames
                .Select(n => _delivery.GetOrAdd(n, s => new BlockingCollection<byte[]>()))
                .ToArray();

            return new MemoryPartitionInbox(queues, queueNames);
        }

        public IQueueWriter GetWriteQueue(string queueName)
        {
            return
                new MemoryQueueWriter(_delivery.GetOrAdd(queueName, s => new BlockingCollection<byte[]>()), queueName);
        }



    }
}