using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Cqrs.Dispatch;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.Partition;

namespace Lokad.Cqrs.Build
{
    public sealed class CqrsEngineBuilder : HideObjectMembersFromIntelliSense
    {
        public readonly IEnvelopeQuarantine Quarantine;
        public readonly MessageDuplicationManager Duplication;
        public readonly IEnvelopeStreamer Streamer;
        public readonly List<IEngineProcess> Processes; 

        public CqrsEngineBuilder(IEnvelopeStreamer streamer, IEnvelopeQuarantine quarantine = null, MessageDuplicationManager duplication = null)
        {
            Processes = new List<IEngineProcess>();
            Streamer = streamer;
            Quarantine = quarantine ?? new MemoryQuarantine();
            Duplication = duplication ?? new MessageDuplicationManager();

            Processes.Add(Duplication);
        }


        public void AddProcess(IEngineProcess process)
        {
            Processes.Add(process);
        }

        
        public void AddProcess(Func<CancellationToken, Task> factoryToStartTask)
        {
            Processes.Add(new TaskProcess(factoryToStartTask));
        }

        public void Dispatch(IPartitionInbox inbox, Action<byte[]> lambda)
        {
            Processes.Add(new DispatcherProcess(lambda, inbox));
        }

        static int _counter = 0;

        public void Handle(IPartitionInbox inbox, Action<ImmutableEnvelope> lambda, string name = null)
        {
            var dispatcherName = name ?? "inbox-" + Interlocked.Increment(ref _counter);
            var dispatcher = new EnvelopeDispatcher(lambda, Streamer, Quarantine, Duplication, dispatcherName);
            AddProcess(new DispatcherProcess(dispatcher.Dispatch, inbox));
        }

        public CqrsEngineHost Build()
        {
            var host = new CqrsEngineHost(Processes.AsReadOnly());
            host.Initialize();
            return host;
        }
    }
}