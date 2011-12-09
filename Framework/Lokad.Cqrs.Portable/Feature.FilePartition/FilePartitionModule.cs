#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.IO;
using System.Linq;
using System.Threading;
using Lokad.Cqrs.Build.Engine;
using Lokad.Cqrs.Core;
using Lokad.Cqrs.Core.Dispatch;
using Lokad.Cqrs.Core.Outbox;
using Lokad.Cqrs.Evil;
using Lokad.Cqrs.Feature.TimerService;

namespace Lokad.Cqrs.Feature.FilePartition
{
    public sealed class FilePartitionModule : HideObjectMembersFromIntelliSense, IAdvancedDispatchBuilder
    {
        readonly FileStorageConfig _fullPath;
        readonly string[] _fileQueues;
        Func<uint, TimeSpan> _decayPolicy;
        
        
        HandlerFactory _dispatcher;
        Func<Container, IEnvelopeQuarantine> _quarantineFactory;

        /// <summary>
        /// Sets the custom decay policy used to throttle File checks, when there are no messages for some time.
        /// This overload eventually slows down requests till the max of <paramref name="maxInterval"/>.
        /// </summary>
        /// <param name="maxInterval">The maximum interval to keep between checks, when there are no messages in the queue.</param>
        public void DecayPolicy(TimeSpan maxInterval)
        {
            _decayPolicy = DecayEvil.BuildExponentialDecay(maxInterval);
        }

        /// <summary>
        /// Sets the custom decay policy used to throttle file queue checks, when there are no messages for some time.
        /// </summary>
        /// <param name="decayPolicy">The decay policy, which is function that returns time to sleep after Nth empty check.</param>
        public void DecayPolicy(Func<uint, TimeSpan> decayPolicy)
        {
            _decayPolicy = decayPolicy;
        }


        public FilePartitionModule(FileStorageConfig fullPath, string[] fileQueues)
        {
            _fullPath = fullPath;
            _fileQueues = fileQueues;


            //DispatchAsEvents();
            DispatcherIsLambda(c => (envelope => {  throw new InvalidOperationException("There was no dispatcher configured");}) );

            Quarantine(c => new MemoryQuarantine());
            DecayPolicy(TimeSpan.FromMilliseconds(100));
        }

        /// <summary>
        /// Defines dispatcher as lambda method that is resolved against the container
        /// </summary>
        /// <param name="factory">The factory.</param>
        public void DispatcherIsLambda(HandlerFactory factory)
        {
            _dispatcher = factory;
        }
        
        public void DispatcherIs(Func<Container, ISingleThreadMessageDispatcher> factory)
        {
            _dispatcher = container =>
                {
                    var d = factory(container);
                    d.Init();
                    return (envelope => d.DispatchMessage(envelope));
                };
        }

        public void Quarantine(Func<Container, IEnvelopeQuarantine> factory)
        {
            _quarantineFactory = factory;
        }

        public void DispatchToRoute(Func<ImmutableEnvelope, string> route)
        {
            DispatcherIs(ctx => new DispatchMessagesToRoute(ctx.Resolve<QueueWriterRegistry>(), route));
        }

        IEngineProcess BuildConsumingProcess(Container context)
        {
            var log = context.Resolve<ISystemObserver>();
            var streamer = context.Resolve<IEnvelopeStreamer>();

            var dispatcher = _dispatcher(context);

            var queues = _fileQueues
                .Select(n => Path.Combine(_fullPath.Folder.FullName, n))
                .Select(n => new DirectoryInfo(n))
                .Select(f => new StatelessFileQueueReader(streamer, log, new Lazy<DirectoryInfo>(() =>
                    {
                        var poison = Path.Combine(f.FullName, "poison");
                        var di = new DirectoryInfo(poison);
                        di.Create();
                        return di;
                    }, LazyThreadSafetyMode.ExecutionAndPublication), f, f.Name))
                .ToArray();
            var inbox = new FilePartitionInbox(queues, _decayPolicy);
            var quarantine = _quarantineFactory(context);
            var manager = context.Resolve<MessageDuplicationManager>();
            var transport = new DispatcherProcess(log, dispatcher, inbox, quarantine, manager, streamer);


            return transport;
        }

        public void Configure(Container container)
        {
            if (null == _dispatcher)
            {
                var message = @"No message dispatcher configured, please supply one.

You can use either 'DispatcherIsLambda' or reference Lokad.CQRS.Composite and 
use Command/Event dispatchers. If you are migrating from v2.0, that's what you 
should do.";
                throw new InvalidOperationException(message);
            }

            var process = BuildConsumingProcess(container);
            var setup = container.Resolve<EngineSetup>();
            setup.AddProcess(process);
        }
    }
}