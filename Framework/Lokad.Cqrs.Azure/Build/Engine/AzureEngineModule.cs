#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Text.RegularExpressions;
using Lokad.Cqrs.Core;
using Lokad.Cqrs.Core.Dispatch;
using Lokad.Cqrs.Core.Outbox;
using Lokad.Cqrs.Feature.AzurePartition;
using Lokad.Cqrs.Feature.AzurePartition.Sender;
using Lokad.Cqrs.Feature.TimerService;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Lokad.Cqrs.Build.Engine
{
    /// <summary>
    /// Autofac syntax for configuring Azure storage
    /// </summary>
    public sealed class AzureEngineModule : HideObjectMembersFromIntelliSense, IFunqlet
    {
        public static readonly Regex QueueName = new Regex("^[A-Za-z][A-Za-z0-9\\-]{2,62}", RegexOptions.Compiled);

        Action<Container> _funqlets = registry => { };

        public void AddAzureSender(IAzureStorageConfig config, string queueName, Action<SendMessageModule> configure)
        {
            var module = new SendMessageModule((context, endpoint) => new AzureQueueWriterFactory(config, context.Resolve<IEnvelopeStreamer>()), config.AccountName, queueName);
            configure(module);
            _funqlets += module.Configure;
        }

        public void AddAzureSender(IAzureStorageConfig config, string queueName)
        {
            AddAzureSender(config, queueName, m => { });
        }

        public void AddAzureProcess(IAzureStorageConfig config, string[] queues, Action<AzurePartitionModule> configure)
        {
            foreach (var queue in queues)
            {
                if (queue.Contains(":"))
                {
                    var message = string.Format("Queue '{0}' should not contain queue prefix, since it's azure already", queue);
                    throw new InvalidOperationException(message);
                }

                if (!QueueName.IsMatch(queue))
                {
                    var format = string.Format("Queue name should match regex '{0}'", QueueName);
                    throw new InvalidOperationException(format);
                }
            }

            var module = new AzurePartitionModule(config, queues);
            configure(module);
            _funqlets += module.Configure;
        }

        [Obsolete("Use overload without handler and container")]
        public void AddAzureProcess(IAzureStorageConfig config, string firstQueue, HandlerFactory handler)
        {
            AddAzureProcess(config, new[] { firstQueue}, m => m.DispatcherIsLambda(handler));
        }

        public void AddAzureProcess(IAzureStorageConfig config, string firstQueue, Action<ImmutableEnvelope> handler, IEnvelopeQuarantine quarantine = null)
        {
            AddAzureProcess(config, new string[] { firstQueue}, m =>
                {
                    m.DispatcherIsLambda(_ => handler);
                    if (null != quarantine)
                    {
                        m.Quarantine( quarantine);
                    }
                });
        }

        public void AddAzureProcess(IAzureStorageConfig config, string firstQueue, Action<AzurePartitionModule> configure)
        {
            AddAzureProcess(config, new[] { firstQueue}, configure);
        }

        public void AddAzureRouter(IAzureStorageConfig config, string queueName, Func<ImmutableEnvelope, string> configure)
        {
            AddAzureProcess(config, new[] {queueName}, m => m.DispatchToRoute(configure));
        }

        public void AddAzureTimer(IAzureStorageConfig config, string incomingQueue, string replyQueue)
        {
            var module = new AzurePartitionModule(config, new[] { incomingQueue });

            module.DispatcherIsLambda(container =>
            {
                var setup = container.Resolve<EngineSetup>();
                var registry = container.Resolve<QueueWriterRegistry>();
                var streamer = container.Resolve<IEnvelopeStreamer>();
                var writer = registry.GetOrAdd(config.AccountName, s => new AzureQueueWriterFactory(config, streamer));
                var queue = writer.GetWriteQueue(replyQueue);

                var root = AzureStorage.CreateStreaming(config);
                var c = root.GetContainer(incomingQueue + "-future").Create();

                var service = new StreamingTimerService(queue, c, streamer);
                setup.AddProcess(service);
                return (envelope => service.PutMessage(envelope));
            });

            _funqlets += module.Configure;
        }

        public void Configure(Container container)
        {
            _funqlets(container);
            
        }
    }
}