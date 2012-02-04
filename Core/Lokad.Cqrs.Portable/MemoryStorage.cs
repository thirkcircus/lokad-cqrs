#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Concurrent;
using System.Linq;
using Lokad.Cqrs.AtomicStorage;
using Lokad.Cqrs.Build;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.Partition;
using Lokad.Cqrs.TapeStorage;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Lokad.Cqrs
{

    public static class MemoryStorage
    {
        /// <summary>
        /// Creates the simplified nuclear storage wrapper around Atomic storage, using the default
        /// storage configuration and atomic strategy.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <returns>
        /// new instance of the nuclear storage
        /// </returns>
        public static NuclearStorage CreateNuclear(this MemoryStorageConfig dictionary)
        {
            return CreateNuclear(dictionary, b => { });
        }

        public static MemoryStorageConfig CreateConfig()
        {
            return new MemoryStorageConfig();
        }

  

        /// <summary>
        /// Creates the simplified nuclear storage wrapper around Atomic storage.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="configStrategy">The config strategy.</param>
        /// <returns></returns>
        public static NuclearStorage CreateNuclear(this MemoryStorageConfig dictionary,
            Action<DefaultAtomicStorageStrategyBuilder> configStrategy)
        {
            var strategyBuilder = new DefaultAtomicStorageStrategyBuilder();
            configStrategy(strategyBuilder);

            var strategy = strategyBuilder.Build();
            return CreateNuclear(dictionary, strategy);
        }


        /// <summary>
        /// Creates the simplified nuclear storage wrapper around Atomic storage.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="strategy">The atomic storage strategy.</param>
        /// <returns></returns>
        public static NuclearStorage CreateNuclear(this MemoryStorageConfig dictionary, IAtomicStorageStrategy strategy)
        {
            var container = new MemoryAtomicContainer(dictionary.Data, strategy);
            container.Initialize();
            return new NuclearStorage(container);
        }

        /// <summary>
        /// Creates the simplified nuclear storage wrapper around Atomic storage, using the provided subfolder.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="strategy">The atomic storage strategy.</param>
        /// <returns></returns>
        public static NuclearStorage CreateNuclear(this MemoryStorageConfig dictionary, IAtomicStorageStrategy strategy, string subFolder)
        {
            var container = new MemoryAtomicContainer(dictionary.Data, strategy, subFolder);
            container.Initialize();
            return new NuclearStorage(container);
        }

        public static MemoryQueueWriterFactory CreateWriteQueueFactory(this MemoryStorageConfig storageConfig)
        {
            return new MemoryQueueWriterFactory(storageConfig);
        }

        /// <summary>
        /// Creates memory-based tape storage factory, using the provided concurrent dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <returns></returns>
        public static MemoryTapeStorageFactory CreateTape(this MemoryStorageConfig storageConfig, string container)
        {
            var factory = new MemoryTapeStorageFactory(storageConfig.Tapes, container);
            factory.InitializeForWriting();
            return factory;
        }


        public static MemoryTapeStorageFactory CreateTape(this MemoryStorageConfig storageConfig)
        {
            var factory = new MemoryTapeStorageFactory(storageConfig.Tapes, null);
            factory.InitializeForWriting();
            return factory;
        }

        public static MemoryPartitionInbox CreateInbox(this MemoryStorageConfig storageConfig,  params string[] queueNames)
        {
            var queues = queueNames
                .Select(n => storageConfig.Queues.GetOrAdd(n, s => new BlockingCollection<byte[]>()))
                .ToArray();

            return new MemoryPartitionInbox(queues, queueNames);
        }

        public static IQueueWriter CreateQueueWriter(this MemoryStorageConfig storageConfig, string queueName)
        {
            var collection = storageConfig.Queues.GetOrAdd(queueName, s => new BlockingCollection<byte[]>());
            return new MemoryQueueWriter(collection, queueName);
        }

        public static SimpleMessageSender CreateSimpleSender(this MemoryStorageConfig storageConfig, IEnvelopeStreamer streamer, string queueName, Func<string> idGenerator = null)
        {
            var queueWriter = new[]{ CreateQueueWriter(storageConfig, queueName)};
            return new SimpleMessageSender(streamer, queueWriter, idGenerator);
        }
    }
}