#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.IO;
using System.Linq;
using Lokad.Cqrs.Build.Engine;
using Lokad.Cqrs.Core;
using Lokad.Cqrs.Core.Dispatch;
using Lokad.Cqrs.Core.Envelope;
using Lokad.Cqrs.Core.Outbox;
using Lokad.Cqrs.Evil;
using Lokad.Cqrs.Feature.MemoryPartition;

namespace Lokad.Cqrs.Feature.FilePartition
{
    public sealed class FilePartitionModule : HideObjectMembersFromIntelliSense, IAdvancedDispatchBuilder
    {
        readonly FileStorageConfig _fullPath;
        readonly string[] _fileQueues;
        Func<uint, TimeSpan> _decayPolicy;
        
        
        Func<Container,  Action<byte[]>> _dispatcher;
         IEnvelopeQuarantine _quarantine;

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

            Quarantine(new MemoryQuarantine());
            DecayPolicy(TimeSpan.FromMilliseconds(100));
        }

        public void DispatcherIs(Func<Container, ISingleThreadMessageDispatcher> factory)
        {
            DispatcherIsLambda(container =>
            {
                var dis = factory(container);
                dis.Init();
                return (envelope => dis.DispatchMessage(envelope));
            });
        }

        /// <summary>
        /// Defines dispatcher as lambda method that is resolved against the container
        /// </summary>
        /// <param name="factory">The factory.</param>
        public void DispatcherIsLambda(HandlerFactory factory)
        {
            _dispatcher = container =>
            {
                var d = factory(container);
                var manager = container.Resolve<MessageDuplicationManager>();
                var streamer = container.Resolve<IEnvelopeStreamer>();
                var observer = container.Resolve<ISystemObserver>();
                var wrapper = new EnvelopeDispatcher(d, _quarantine, manager, streamer, observer);
                return (buffer => wrapper.Dispatch(buffer));
            };
        }

        public void Quarantine(IEnvelopeQuarantine factory)
        {
            _quarantine = factory;
        }

        public void DispatchToRoute(Func<ImmutableEnvelope, string> route)
        {
            DispatcherIs(ctx => new DispatchMessagesToRoute(ctx.Resolve<QueueWriterRegistry>(), route, ctx.Resolve<IEnvelopeStreamer>()));
        }

        IEngineProcess BuildConsumingProcess(Container context)
        {
            var log = context.Resolve<ISystemObserver>();

            var dispatcher = _dispatcher(context);

            var queues = _fileQueues
                .Select(n => Path.Combine(_fullPath.Folder.FullName, n))
                .Select(n => new DirectoryInfo(n))
                .Select(f => new StatelessFileQueueReader(log, f, f.Name))
                .ToArray();
            var inbox = new FilePartitionInbox(queues, _decayPolicy);
            var transport = new DispatcherProcess(log, dispatcher, inbox);


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