using System;
using System.Collections.Generic;
using Lokad.Cqrs;
using Lokad.Cqrs.AtomicStorage;
using Lokad.Cqrs.Build;
using Lokad.Cqrs.Partition;
using Lokad.Cqrs.StreamingStorage;
using Lokad.Cqrs.TapeStorage;

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
            var nuclear = CreateNuclear(new DocumentStrategy());
            var docs = nuclear.Container;
            var routerQueue = CreateQueueWriter(Topology.RouterQueue);

            var command = new RedirectToCommand();

            var eventStore = new TapeStreamEventStore(Tapes, Streamer, routerQueue);
            DomainBoundedContext.ApplicationServices(docs,  eventStore).ForEach(command.WireToWhen);

            
            var sender = new SimpleMessageSender(Streamer, routerQueue);
            var flow = new MessageSender(sender);

            var builder = new CqrsEngineBuilder(Streamer);


            builder.Handle(CreateInbox(Topology.RouterQueue),Topology.Route(CreateQueueWriter, Streamer, Tapes), "router");
            builder.Handle(CreateInbox(Topology.EntityQueue), em => CallHandlers(command, em));

            var functions = new RedirectToDynamicEvent();
            // documents
            //functions.WireToWhen(new RegistrationUniquenessProjection(atomic.Factory.GetEntityWriter<unit, RegistrationUniquenessDocument>()));

            
            ClientBoundedContext.Projections(docs).ForEach(functions.WireToWhen);
            DomainBoundedContext.Receptors(flow).ForEach(functions.WireToWhen);

            builder.Handle(CreateInbox(Topology.EventsQueue), aem => CallHandlers(functions, aem));

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

    public static class ExtendArrayEvil
    {
        public static void ForEach<T>(this IEnumerable<T> self, Action<T> action)
        {
            foreach (var variable in self)
            {
                action(variable);
            }
        }
    }
}