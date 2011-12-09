using System;
using System.IO;
using Lokad.Cqrs.Core;
using Lokad.Cqrs.Core.Dispatch;
using Lokad.Cqrs.Core.Outbox;
using Lokad.Cqrs.Feature.FilePartition;
using Lokad.Cqrs.Feature.StreamingStorage;
using Lokad.Cqrs.Feature.TimerService;

namespace Lokad.Cqrs.Build.Engine
{
    public sealed class FileModule : HideObjectMembersFromIntelliSense
    {
        Action<Container> _funqlets = registry => { };
        public void Configure(Container componentRegistry)
        {
            _funqlets(componentRegistry);
        }

        public void AddFileProcess(FileStorageConfig folder, string[] queues, Action<FilePartitionModule> config)
        {
            var module = new FilePartitionModule(folder, queues);
            config(module);
            _funqlets += module.Configure;
        }
        [Obsolete("Use overload without handler and container")]
        public void AddFileProcess(FileStorageConfig folder, string queues, HandlerFactory handler)
        {
            AddFileProcess(folder, queues, m => m.DispatcherIsLambda(handler));
        }

        public void AddFileProcess(FileStorageConfig config, string firstQueue, Action<ImmutableEnvelope> handler, IEnvelopeQuarantine quarantine = null)
        {
            AddFileProcess(config, new string[] { firstQueue }, m =>
            {
                m.DispatcherIsLambda(_ => handler);
                if (null != quarantine)
                {
                    m.Quarantine(_ => quarantine);
                }
            });
        }

        public void AddFileTimer(FileStorageConfig folder, string incomingQueue, string replyQueue)
        {
            var module = new FilePartitionModule(folder, new[] {incomingQueue});
            
            module.DispatcherIsLambda(container =>
                {
                    var setup = container.Resolve<EngineSetup>();
                    var registry = container.Resolve<QueueWriterRegistry>();
                    var streamer = container.Resolve<IEnvelopeStreamer>();
                    var writer = registry.GetOrAdd(folder.AccountName, s => new FileQueueWriterFactory(folder));
                    var queue = writer.GetWriteQueue(replyQueue);
                    var storage = Path.Combine(folder.FullPath, incomingQueue + "-future");

                    var c = new FileStreamingContainer(storage);
                    var service = new StreamingTimerService(queue, c, streamer);
                    setup.AddProcess(service);
                    return (envelope => service.PutMessage(envelope));
                });
            _funqlets += module.Configure;
        }

        public void AddFileSender(FileStorageConfig folder, string queueName)
        {
            AddFileSender(folder, queueName, module => { });
        }

        public void AddFileProcess(FileStorageConfig folder, string queueName, Action<FilePartitionModule> config)
        {
            AddFileProcess(folder, new[] { queueName }, config);
        }

        public void AddFileRouter(FileStorageConfig folder, string queueName, Func<ImmutableEnvelope, string> config)
        {
            AddFileProcess(folder, queueName, m => m.DispatchToRoute(config));
        }

        public void AddFileRouter(FileStorageConfig folder, string[] queueNames, Func<ImmutableEnvelope, string> config)
        {
            AddFileProcess(folder, queueNames, m => m.DispatchToRoute(config));
        }

        public void AddFileSender(FileStorageConfig directory, string queueName, Action<SendMessageModule> config)
        {
            var module = new SendMessageModule((context, s) => new FileQueueWriterFactory(directory), directory.AccountName, queueName);
            config(module);
            _funqlets += module.Configure;
        }
    }
}