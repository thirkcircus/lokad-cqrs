#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.IO;
using Lokad.Cqrs.AtomicStorage;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.Evil;
using Lokad.Cqrs.Partition;
using Lokad.Cqrs.StreamingStorage;
using Lokad.Cqrs.TapeStorage;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Lokad.Cqrs
{
    public static class FileStorage
    {
        public static NuclearStorage CreateNuclear(this FileStorageConfig config, IAtomicStorageStrategy strategy)
        {
            var factory = new FileAtomicStorageFactory(config.FullPath, strategy);
            
            return new NuclearStorage(factory);
        }


        public static NuclearStorage CreateNuclear(this FileStorageConfig self, Action<DefaultAtomicStorageStrategyBuilder> config)
        {
            var strategyBuilder = new DefaultAtomicStorageStrategyBuilder();
            config(strategyBuilder);
            var strategy = strategyBuilder.Build();
            return CreateNuclear(self, strategy);
        }

        public static NuclearStorage CreateNuclear(this FileStorageConfig config)
        {
            return CreateNuclear(config, builder => { });
        }


        public static IStreamingRoot CreateStreaming(this FileStorageConfig config)
        {
            var path = config.FullPath;
            var container = new FileStreamingContainer(path);
            container.Create();
            return container;
        }
        public static IStreamingContainer CreateStreaming(this FileStorageConfig config, string subfolder)
        {
            return config.CreateStreaming(subfolder).Create();
        }
        
        public static FileTapeStorageFactory CreateTape(this FileStorageConfig config, string subfolder = "tapes") 
        {
            var path = Path.Combine(config.FullPath, subfolder);
            var factory = new FileTapeStorageFactory(path);
            factory.InitializeForWriting();
            return factory;
        }


        public static FileStorageConfig CreateConfig(string fullPath, string optionalName = null, bool reset = false)
        {
            var folder = new DirectoryInfo(fullPath);
            var config = new FileStorageConfig(folder, optionalName ?? folder.Name);
            if (reset)
            {
                config.Reset();
            }
            return config;
        }

        public static FileStorageConfig CreateConfig(DirectoryInfo info, string optionalName = null)
        {
            return new FileStorageConfig(info, optionalName ?? info.Name);
        }

        public static FilePartitionInbox CreateInbox(this FileStorageConfig cfg, string name, Func<uint, TimeSpan> decay = null)
        {
            var reader = new StatelessFileQueueReader(Path.Combine(cfg.FullPath, name), name);

            var waiter = decay ?? DecayEvil.BuildExponentialDecay(250);
            var inbox = new FilePartitionInbox(new[]{reader, }, waiter);
            inbox.Init();
            return inbox;
        }
        public static FileQueueWriter CreateQueueWriter(this FileStorageConfig cfg, string queueName)
        {
            var full = Path.Combine(cfg.Folder.FullName, queueName);
            if (!Directory.Exists(full))
            {
                Directory.CreateDirectory(full);
            }
            return
                new FileQueueWriter(new DirectoryInfo(full), queueName);
        }

        public static SimpleMessageSender CreateSimpleSender(this FileStorageConfig account, IEnvelopeStreamer streamer, string queueName)
        {
            return new SimpleMessageSender(streamer, CreateQueueWriter(account, queueName));
        }
    }
}