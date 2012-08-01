using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lokad.Cqrs;
using Lokad.Cqrs.AtomicStorage;
using Lokad.Cqrs.Build;
using Lokad.Cqrs.Partition;
using Lokad.Cqrs.StreamingStorage;
using Lokad.Cqrs.TapeStorage;
using SaaS.Client;
using SaaS.Engine;

namespace SaaS.Wires
{
    public sealed class Setup
    {
        public IStreamRoot Streaming;
        public Func<string, IAppendOnlyStore> CreateTapes;
        public IDocumentStore Docs;

        public Func<string, IQueueWriter> CreateQueueWriter;
        public Func<string, IPartitionInbox> CreateInbox;
        


        public readonly IEnvelopeStreamer Streamer = Contracts.CreateStreamer();
        public readonly IDocumentStrategy Strategy = new DocumentStrategy();

        public Container BuildContainer()
        {
            // set up all the variables
               var tapes = CreateTapes(Topology.TapesContainer);
            var routerQueue = CreateQueueWriter(Topology.RouterQueue);

            var commands = new RedirectToCommand();
            var events = new RedirectToDynamicEvent();
            

            var eventStore = new EventStore(tapes, Streamer, routerQueue);
            var simple = new SimpleMessageSender(Streamer, routerQueue);
            var flow = new CommandSender(simple);
            var builder = new CqrsEngineBuilder(Streamer);
            var projections = new ProjectionsConsumingOneBoundedContext();

            // route queue infrastructure together
            builder.Handle(CreateInbox(Topology.RouterQueue), Topology.Route(CreateQueueWriter, Streamer, tapes), "router");
            builder.Handle(CreateInbox(Topology.EntityQueue), em => CallHandlers(commands, em));
            builder.Handle(CreateInbox(Topology.EventsQueue), aem => CallHandlers(events, aem));


            // message wiring magic
            DomainBoundedContext.ApplicationServices(Docs, eventStore).ForEach(commands.WireToWhen);
            DomainBoundedContext.Receptors(flow).ForEach(events.WireToWhen);
            DomainBoundedContext.Tasks(flow, Docs, false).ForEach(builder.AddTask);
            projections.RegisterFactory(DomainBoundedContext.Projections);

            projections.RegisterFactory(ClientBoundedContext.Projections);

            projections.BuildFor(Docs).ForEach(events.WireToWhen);

            return new Container
                {
                    Builder = builder,
                    Sender = flow,
                    Setup = this,
                    Simple = simple,
                    AppendOnlyStore = tapes,
                    ProjectionFactories = projections
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
        }

        static void CallHandlers(RedirectToCommand serviceCommands, ImmutableEnvelope aem)
        {
            var content = aem.Items[0].Content;
            serviceCommands.Invoke(content);
        }


        /// <summary>
        /// Helper class that merely makes the concept explicit
        /// </summary>
        public sealed class ProjectionsConsumingOneBoundedContext
        {
            public delegate IEnumerable<object> FactoryForWhenProjections(IDocumentStore store);

            readonly IList<FactoryForWhenProjections> _factories = new List<FactoryForWhenProjections>();

            public void RegisterFactory(FactoryForWhenProjections factory)
            {
                _factories.Add(factory);
            }

            public IEnumerable<object> BuildFor(IDocumentStore store)
            {
                return _factories.SelectMany(factory => factory(store));
            }
        }

    }

    public sealed class Container : IDisposable
    {
        public Setup Setup;
        public CqrsEngineBuilder Builder;
        public ICommandSender Sender;
        public SimpleMessageSender Simple;
        public IAppendOnlyStore AppendOnlyStore;
        public Setup.ProjectionsConsumingOneBoundedContext ProjectionFactories;

        public void ExecuteStartupTasks(CancellationToken token)
        {
            // we run S2 projections from 3 different BCs against one domain log
            StartupProjectionRebuilder.Rebuild(
                token,
                Setup.Docs,
                AppendOnlyStore, 
                store => ProjectionFactories.BuildFor(store));
        }

        public void Dispose()
        {
            using (AppendOnlyStore)
            {
                AppendOnlyStore.Close();
            }
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