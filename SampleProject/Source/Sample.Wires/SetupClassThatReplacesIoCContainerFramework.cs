using System;
using System.Collections.Generic;
using Lokad.Cqrs;
using Lokad.Cqrs.AtomicStorage;
using Lokad.Cqrs.Build;
using Lokad.Cqrs.Partition;
using Lokad.Cqrs.StreamingStorage;
using Lokad.Cqrs.TapeStorage;
using Sample.Engine;
using Sample.Projections;

namespace Sample.Wires
{
    public sealed class SetupClassThatReplacesIoCContainerFramework
    {
        public IStreamRoot Streaming;
        public ITapeContainer Tapes;
        public Func<string, IQueueWriter> CreateQueueWriter;
        public Func<string, IPartitionInbox> CreateInbox;
        public Func<IDocumentStrategy, NuclearStorage> CreateNuclear;


        public IEnvelopeStreamer Streamer = Contracts.CreateStreamer();

        public sealed class AssembledComponents
        {
            public SetupClassThatReplacesIoCContainerFramework Setup;
            public CqrsEngineBuilder Builder;
            public SimpleMessageSender Sender;
        }

        public AssembledComponents AssembleComponents()
        {
            // set up all the variables
            var nuclear = CreateNuclear(new DocumentStrategy());
            var docs = nuclear.Container;
            var routerQueue = CreateQueueWriter(Topology.RouterQueue);

            var commands = new RedirectToCommand();
            var events = new RedirectToDynamicEvent();

            var eventStore = new TapeStreamEventStore(Tapes, Streamer, routerQueue);
            var sender = new SimpleMessageSender(Streamer, routerQueue);
            var flow = new MessageSender(sender);
            var builder = new CqrsEngineBuilder(Streamer);

            // route queue infrastructure together
            builder.Handle(CreateInbox(Topology.RouterQueue), Topology.Route(CreateQueueWriter, Streamer, Tapes), "router");
            builder.Handle(CreateInbox(Topology.EntityQueue), em => CallHandlers(commands, em));
            builder.Handle(CreateInbox(Topology.EventsQueue), aem => CallHandlers(events, aem));


            // message wiring magic
            DomainBoundedContext.ApplicationServices(docs, eventStore).ForEach(commands.WireToWhen);
            DomainBoundedContext.Receptors(flow).ForEach(events.WireToWhen);
            DomainBoundedContext.Projections(docs).ForEach(events.WireToWhen);

            ClientBoundedContext.Projections(docs).ForEach(events.WireToWhen);

            return new AssembledComponents
                {
                    Builder = builder,
                    Sender = sender,
                    Setup = this
                };
        }

        static void CallHandlers(RedirectToDynamicEvent functions, ImmutableEnvelope aem)
        {
            if (aem.Items.Length != 1)
                throw new InvalidOperationException(
                    "Unexpected number of items in envelope that arrived to projections: " +
                        aem.Items.Length);
            // we wire envelope contents to both direct message call and sourced call (with date wrapper)
            var content = aem.Items[0].Content;
            functions.InvokeEvent(content);
            functions.InvokeEvent(Source.For(aem.EnvelopeId, aem.CreatedOnUtc, (ISampleEvent) content));
        }

        static void CallHandlers(RedirectToCommand serviceCommands, ImmutableEnvelope aem)
        {
            var content = aem.Items[0].Content;
            serviceCommands.Invoke(content);
        }
    }
}