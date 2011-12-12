#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;
using System.Collections.Generic;
using Lokad.Cqrs.Core.Reactive;
using Lokad.Cqrs.Feature.MemoryPartition;

// ReSharper disable UnusedMethodReturnValue.Global

namespace Lokad.Cqrs.Build.Engine
{
    /// <summary>
    /// Fluent API for creating and configuring <see cref="CqrsEngineHost"/>
    /// </summary>
    public class CqrsEngineBuilder : HideObjectMembersFromIntelliSense
    {
      //  readonly StorageModule _storage;


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
            _setup = new EngineSetup();

            // snap in-memory stuff

            var memoryAccount = new MemoryAccount();
            _setup.Registry.Add(new MemoryQueueWriterFactory(memoryAccount));


         //   _storage = new StorageModule(_observer);
        }


        readonly List<IObserver<ISystemEvent>> _observers = new List<IObserver<ISystemEvent>>
            {
                new ImmediateTracingObserver()
            };


        public EngineSetup Setup
        {
            get { return _setup; }
        }


        public IList<IObserver<ISystemEvent>> Observers
        {
            get { return _observers; }
        }




        readonly EngineSetup _setup;


        /// <summary>
        /// Builds this <see cref="CqrsEngineHost"/>.
        /// </summary>
        /// <returns>new instance of cloud engine host</returns>
        public CqrsEngineHost Build()
        {
            // swap post-init observers into the place
            

            ExecuteAlterConfiguration();

            var host = new CqrsEngineHost(_setup.GetProcesses());
            host.Initialize();

            ExecuteStartupTasks(host);

            return host;
        }
    }
}