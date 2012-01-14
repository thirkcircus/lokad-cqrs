using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Lokad.Cqrs;
using Sample.Wires;

namespace Sample.Engine
{

    class Program
    {
        static void Main(string[] args)
        {
            ConfigureObserver();

            const string integrationPath = @"temp";
            var config = FileStorage.CreateConfig(integrationPath, "files");

            var startupMessages = new List<ISampleMessage>();
            var proc = Process.GetCurrentProcess();
            startupMessages.Add(new InstanceStarted("Inject git rev", proc.ProcessName,
                proc.Id.ToString()));


            {
                Console.WriteLine("Starting in funny mode by wiping store and sending a few messages");
                config.Reset();
                startupMessages.AddRange(DemoMessages.Create());
            }


            var setup = new SetupClassThatReplacesIoCContainerFramework
                {
                    CreateNuclear = strategy => config.CreateNuclear(strategy),
                    Streaming = config.CreateStreaming(),
                    Tapes = config.CreateTape(Topology.TapesContainer),
                    CreateInbox = s => config.CreateInbox(s),
                    CreateQueueWriter = s => config.CreateQueueWriter(s),
                };

            var components = setup.AssembleComponents();


            using (var cts = new CancellationTokenSource())
            using (var engine = components.Builder.Build())
            {
                var task = engine.Start(cts.Token);

                foreach (var sampleMessage in startupMessages)
                {
                    components.Sender.SendOne(sampleMessage);
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
    }
}