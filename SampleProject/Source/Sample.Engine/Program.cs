#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2012 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD License
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Lokad.Cqrs;
using Lokad.Cqrs.Build;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.Envelope.Events;
using Lokad.Cqrs.TimerService;
using Sample.Processes;
using Sample.Projections;
using Sample.Wires;

namespace Sample.Engine
{
    class Program
    {
        

        static void Main(string[] args)
        {
            const string integrationPath = @"temp";
            ConfigureObserver();

            var config = FileStorage.CreateConfig(integrationPath, "files");

            var demoMessages = new List<ISampleMessage>();
            var currentProcess = Process.GetCurrentProcess();
            demoMessages.Add(new InstanceStarted("Inject git rev", currentProcess.ProcessName,
                currentProcess.Id.ToString()));


            {
                // wipe all folders
                config.Reset();
                Console.WriteLine("Starting in funny mode");
                var security = new SecurityId(0);
                demoMessages.Add(new CreateSecurityAggregate(security));
                demoMessages.Add(new AddSecurityPassword(security, "Rinat Abdullin", "contact@lokad.com", "password"));
                demoMessages.Add(new AddSecurityIdentity(security, "Rinat's Open ID", "http://abdullin.myopenid.org"));
                demoMessages.Add(new AddSecurityKey(security, "some key"));
            }



            var atomic = config.CreateNuclear(new DocumentStrategy());
            var identity = new IdentityGenerator(atomic);
            var streamer = Contracts.CreateStreamer();

            var tapes = config.CreateTape(Topology.TapesContainer);
            var streaming = config.CreateStreaming();
            var routerQueue = config.CreateQueueWriter(Topology.RouterQueue);
            var aggregates = new AggregateFactory(tapes, streamer, routerQueue, atomic, identity);

            var sender = new SimpleMessageSender(streamer, routerQueue);
            var flow = new MessageSender(sender);

            var builder = new CqrsEngineBuilder(streamer);


            builder.Handle(config.CreateInbox(Topology.RouterQueue),
                Topology.Route(config.CreateQueueWriter, streamer, tapes), "router");
            builder.Handle(config.CreateInbox(Topology.EntityQueue), aggregates.Dispatch);

            var functions = new RedirectToDynamicEvent();
            // documents
            //functions.WireToWhen(new RegistrationUniquenessProjection(atomic.Factory.GetEntityWriter<unit, RegistrationUniquenessDocument>()));

            // UI projections
            var projectionStore = config.CreateNuclear(new ProjectionStrategy());
            foreach (var projection in BootstrapProjections.BuildProjectionsWithWhenConvention(projectionStore.Factory))
            {
                functions.WireToWhen(projection);
            }

            // processes
            //functions.WireToWhen(new BillingProcess(flow));
            //functions.WireToWhen(new RegistrationProcess(flow));
            functions.WireToWhen(new ReplicationProcess(flow));

            builder.Handle(config.CreateInbox(Topology.EventsQueue), aem => CallHandlers(functions, aem));


            var timer = new StreamingTimerService(config.CreateQueueWriter(Topology.RouterQueue),
                streaming.GetContainer(Topology.FutureMessagesContainer), streamer);
            builder.Handle(config.CreateInbox(Topology.TimerQueue), timer.PutMessage);
            builder.AddProcess(timer);


            using (var cts = new CancellationTokenSource())
            using (var engine = builder.Build())
            {

                var task = engine.Start(cts.Token);

                foreach (var sampleMessage in demoMessages)
                {
                    sender.SendOne(sampleMessage);
                }

                Console.WriteLine(@"Press enter to stop");
                Console.ReadLine();
                cts.Cancel();



                if (task.Wait(5000))
                {
                    Console.WriteLine(@"Terminating");
                }
            }
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


        static void ConfigureObserver()
        {
            // Just for debugging, we print everything from the trace to console
            Trace.Listeners.Add(new ConsoleTraceListener());

            // Plug custom observer that will handle only specific system events
            SystemObserver.Swap(new MyConsoleObserver());

            // replace MyConsoleObserver with ImmediateConsoleObserver 
            // to display all system events see the difference :)
            // SystemObserver.Swap(new ImmediateConsoleObserver());
        }

        sealed class MyConsoleObserver : IObserver<ISystemEvent>
        {
            readonly Stopwatch _watch = Stopwatch.StartNew();

            public void OnNext(ISystemEvent value)
            {
                RedirectToWhen.InvokeEventOptional(this, value);
            }

            void When(EnvelopeDispatched ed)
            {
                if (ed.Dispatcher == "router")
                {
                    foreach (var item in ed.Envelope.Items)
                    {
                        var prefix = "";
                        if (item.Content is ICommand<IIdentity>)
                        {
                            prefix = ((ICommand<IIdentity>) (item.Content)).Id + " ";
                        }
                        else if (item.Content is IEvent<IIdentity>)
                        {
                            prefix = ((IEvent<IIdentity>) (item.Content)).Id + " ";
                        }
                        WriteLine(prefix + Describe.Object(item.Content));
                    }
                }
            }

            void When(EnvelopeQuarantined e)
            {
                WriteLine(e.LastException.ToString());
            }

            void WriteLine(string line)
            {
                Console.WriteLine("[{0:0000000}]: {1}", _watch.ElapsedMilliseconds, line);
            }


            public void OnError(Exception error) {}

            public void OnCompleted() {}
        }
    }
}