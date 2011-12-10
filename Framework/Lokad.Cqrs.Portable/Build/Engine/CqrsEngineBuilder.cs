#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;
using System.Collections.Generic;
using Lokad.Cqrs.Core;
using Lokad.Cqrs.Core.Dispatch;
using Lokad.Cqrs.Core.Envelope;
using Lokad.Cqrs.Core.Outbox;
using Lokad.Cqrs.Core.Reactive;
using Lokad.Cqrs.Core.Serialization;
using Lokad.Cqrs.Feature.MemoryPartition;

// ReSharper disable UnusedMethodReturnValue.Global

namespace Lokad.Cqrs.Build.Engine
{
    /// <summary>
    /// Fluent API for creating and configuring <see cref="CqrsEngineHost"/>
    /// </summary>
    public class CqrsEngineBuilder : HideObjectMembersFromIntelliSense, IAdvancedEngineBuilder
    {
        IEnvelopeSerializer _envelopeSerializer = new EnvelopeSerializerWithDataContracts();
        Func<Type[], IDataSerializer> _dataSerializer;
        readonly StorageModule _storage;
        readonly SystemObserver _observer;
        readonly Container _messyWires = new Container();

        /// <summary>
        /// Tasks that are executed after engine is initialized and before starting up
        /// </summary>
        public List<IEngineStartupTask> StartupTasks = new List<IEngineStartupTask>();

        /// <summary>
        /// Tasks that are executed before engine is being built
        /// </summary>
        public List<IAfterConfigurationTask> AfterConfigurationTasks = new List<IAfterConfigurationTask>();

        void ExecuteAlterConfiguration()
        {
            foreach (var task in AfterConfigurationTasks)
            {
                task.Execute(this);
            }
        }

        void ExecuteStartupTasks(CqrsEngineHost host)
        {
            foreach (var task in StartupTasks)
            {
                task.Execute(host);
            }
        }

        public CqrsEngineBuilder()
        {
            // init time observer
            _observer = new SystemObserver(new ImmediateTracingObserver());
            _setup = new EngineSetup(_observer);

            // snap in-memory stuff

            var memoryAccount = new MemoryAccount();
            _setup.Registry.Add(new MemoryQueueWriterFactory(memoryAccount));
            _messyWires.Register(memoryAccount);


            _dataSerializer = types => new DataSerializerWithDataContracts(types);
            _storage = new StorageModule(_observer);
        }

        /// <summary>
        /// <para>Lightweight message configuration that wires in message contract classes, 
        /// assuming message handlers will be defined as lambdas.</para>
        /// <para>See <see cref="MessagesWithHandlers"/> if you are used to handler classes
        /// that inherit from one or more interfaces that look like: <c>IConsume[SomeMessage]</c>.</para> 
        /// </summary>
        /// <param name="config">The config.</param>
        public void Messages(Action<MessagesConfigurationSyntax> config)
        {
            var mlm = new MessagesConfigurationSyntax();
            config(mlm);
            _serializationTypes.AddRange(mlm.LookupMessages());
        }

        /// <summary>
        /// <para>Lightweight message configuration that wires in message contract classes, 
        /// assuming message handlers will be defined as lambdas.</para>
        /// <para>See <see cref="MessagesWithHandlers"/> if you are used to handler classes
        /// that inherit from one or more interfaces that look like: <c>IConsume[SomeMessage]</c>.</para> 
        /// </summary>
        /// <param name="messageTypes">The message types.</param>
        public void Messages(IEnumerable<Type> messageTypes)
        {
            _serializationTypes.AddRange(messageTypes);
        }




        readonly IList<Func<Container, IQueueWriterFactory>> _activators =
            new List<Func<Container, IQueueWriterFactory>>();

        readonly List<IObserver<ISystemEvent>> _observers = new List<IObserver<ISystemEvent>>
            {
                new ImmediateTracingObserver()
            };


        void IAdvancedEngineBuilder.CustomDataSerializer(Func<Type[], IDataSerializer> serializer)
        {
            _dataSerializer = serializer;
        }

        public EngineSetup Setup
        {
            get { return _setup; }
        }

        void IAdvancedEngineBuilder.CustomEnvelopeSerializer(IEnvelopeSerializer serializer)
        {
            _envelopeSerializer = serializer;
        }

        void IAdvancedEngineBuilder.RegisterQueueWriterFactory(Func<Container, IQueueWriterFactory> activator)
        {
            _activators.Add(activator);
        }

        Action<Container> _moduleEnlistments = container => { };


        void IAdvancedEngineBuilder.RegisterModule(IFunqlet module)
        {
            _moduleEnlistments += module.Configure;
        }


        void IAdvancedEngineBuilder.ConfigureContainer(Action<Container> build)
        {
            _moduleEnlistments += build;
        }

        IList<IObserver<ISystemEvent>> IAdvancedEngineBuilder.Observers
        {
            get { return _observers; }
        }


        /// <summary>
        /// Allows to configure in-memory queues and processing
        /// </summary>
        /// <param name="configure">The configuration syntax.</param>
        public void Memory(Action<MemoryModule> configure)
        {
            var m = new MemoryModule();
            configure(m);
            _moduleEnlistments += m.Configure;
        }

        /// <summary>
        /// Allows to configure file queues and processing
        /// </summary>
        /// <param name="configure">The configure.</param>
        public void File(Action<FileModule> configure)
        {
            var m = new FileModule();
            configure(m);
            _moduleEnlistments += m.Configure;
        }

        /// <summary>
        /// Adds configuration to the storage module.
        /// </summary>
        /// <param name="configure">The configure.</param>
        public void Storage(Action<StorageModule> configure)
        {
            configure(_storage);
        }

        readonly EngineSetup _setup;


        /// <summary>
        /// Builds this <see cref="CqrsEngineHost"/>.
        /// </summary>
        /// <returns>new instance of cloud engine host</returns>
        public CqrsEngineHost Build()
        {
            _messyWires.Register(_setup);

            // swap post-init observers into the place
            _setup.Observer.Swap(_observers.ToArray());
            _messyWires.Register<ISystemObserver>(_observer);


            // domain should go before serialization
            _storage.Configure(_messyWires);

            if (_serializationTypes.Count == 0)
            {
                // default scan if nothing specified
                Messages(m => { });
            }
            if (_serializationTypes.Count == 0)
            {
                _observer.Notify(new ConfigurationWarningEncountered("No message contracts provided."));
            }
            var dataSerializer = _dataSerializer(_serializationTypes.ToArray());
            var streamer = new EnvelopeStreamer(_envelopeSerializer, dataSerializer);

            _messyWires.Register(BuildRegistry);
            _messyWires.Register(dataSerializer);
            _messyWires.Register<IEnvelopeStreamer>(streamer);
            _messyWires.Register(new MessageDuplicationManager());
            _moduleEnlistments(_messyWires);

            ExecuteAlterConfiguration();

            var host = new CqrsEngineHost(_setup.Observer, _setup.GetProcesses());
            host.Initialize();

            ExecuteStartupTasks(host);

            return host;
        }

        readonly List<Type> _serializationTypes = new List<Type>();

        QueueWriterRegistry BuildRegistry(Container c)
        {
            foreach (var activator in _activators)
            {
                var factory = activator(c);
                Setup.Registry.Add(factory);
            }
            return Setup.Registry;
        }

        public IAdvancedEngineBuilder Advanced
        {
            get { return this; }
        }
    }
}