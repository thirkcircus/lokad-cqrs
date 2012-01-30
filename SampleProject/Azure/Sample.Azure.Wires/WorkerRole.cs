using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Lokad.Cqrs;
using Lokad.Cqrs.Build;
using Microsoft.WindowsAzure.ServiceRuntime;
using Sample.Engine;
using Sample.Wires;

namespace Sample.Azure.Engine
{
    public class WorkerRole : RoleEntryPoint
    {
        CqrsEngineHost _host;

        public override void Run()
        {
            _host.Start(_source.Token);
            _source.Token.WaitHandle.WaitOne();
        }

        readonly CancellationTokenSource _source = new CancellationTokenSource();

        public override bool OnStart()
        {
            ServicePointManager.DefaultConnectionLimit = 48;
            ConfigureObserver();
            var conn = AzureSettingsProvider.GetStringOrThrow("DataConnection");
            var config = AzureStorage.CreateConfig(conn);


            var startupMessages = new List<ISampleMessage>();

            var roleName = RoleEnvironment.CurrentRoleInstance.Role.Name;
            var instanceId = RoleEnvironment.CurrentRoleInstance.Id;

            startupMessages.Add(new InstanceStarted("Inject git rev", roleName, instanceId));

            {
                Console.WriteLine("Starting in funny mode by wiping store and sending a few messages");
                WipeAzureAccount.Fast(s => s.StartsWith("sample-"), config);
                startupMessages.AddRange(DemoMessages.Create());
            }


            var setup = new SetupClassThatReplacesIoCContainerFramework
                {
                    CreateNuclear = strategy => config.CreateNuclear(strategy, "views"),
                    Streaming = config.CreateStreaming(),
                    Tapes = config.CreateTape(Topology.TapesContainer),
                    CreateInbox = s => config.CreateInbox(s),
                    CreateQueueWriter = s => config.CreateQueueWriter(s),
                };

            var components = setup.AssembleComponents();


            _host = components.Builder.Build();

            foreach (var message in startupMessages)
            {
                components.Sender.SendOne(message);
            }


            return base.OnStart();
        }


        public override void OnStop()
        {
            _source.Cancel();
            base.OnStop();
        }

        static void ConfigureObserver()
        {
            // Plug custom observer that will handle only specific system events
            SystemObserver.Swap(new MyTraceObserver());

            // replace MyConsoleObserver with ImmediateConsoleObserver 
            // to display all system events see the difference :)
            // SystemObserver.Swap(new ImmediateConsoleObserver());

            // alternatively you can plug ZeroMQ to publish these lines
            // to all subscribers
        }
    }
}