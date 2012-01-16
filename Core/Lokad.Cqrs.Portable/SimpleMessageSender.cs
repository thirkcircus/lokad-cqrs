#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;
using System.Linq;
using System.Transactions;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.Partition;

namespace Lokad.Cqrs
{
    public sealed class SimpleMessageSender 
    {
        readonly IQueueWriter[] _queues;
        readonly Func<string> _idGenerator;
        readonly IEnvelopeStreamer _streamer;

        public SimpleMessageSender(IEnvelopeStreamer streamer, IQueueWriter[] queues, Func<string> idGenerator = null)
        {
            _queues = queues;
            _idGenerator = idGenerator ?? (() =>Guid.NewGuid().ToString());
            _streamer = streamer;

            if (queues.Length == 0)
                throw new InvalidOperationException("There should be at least one queue");
        }

        public SimpleMessageSender(IEnvelopeStreamer streamer, params IQueueWriter[] queues) : this(streamer, queues,null) {}

        public void SendOne(object content)
        {
            InnerSendBatch(cb => { }, new[] {content});
        }

        public void SendOne(object content, Action<EnvelopeBuilder> configure)
        {
            InnerSendBatch(configure, new[] {content});
        }


        public void SendBatch(object[] content)
        {
            if (content.Length == 0)
                return;

            InnerSendBatch(cb => { }, content);
        }

        public void SendBatch(object[] content, Action<EnvelopeBuilder> builder)
        {
            InnerSendBatch(builder, content);
        }

        public void SendControl(Action<EnvelopeBuilder> builder)
        {
            InnerSendBatch(builder, new object[0]);
        }


        readonly Random _random = new Random();


        void InnerSendBatch(Action<EnvelopeBuilder> configure, object[] messageItems)
        {
            var id = _idGenerator();

            var builder = new EnvelopeBuilder(id);
            foreach (var item in messageItems)
            {
                builder.AddItem(item);
            }

            configure(builder);
            var envelope = builder.Build();

            SendEnvelope(envelope);
        }

        public void SendEnvelope(ImmutableEnvelope envelope)
        {
            var queue = GetOutboundQueue();
            var data = _streamer.SaveEnvelopeData(envelope);

            if (Transaction.Current == null)
            {
                queue.PutMessage(data);

                SystemObserver.Notify(new EnvelopeSent(queue.Name, envelope.EnvelopeId, false,
                    envelope.Items.Select(x => x.MappedType.Name).ToArray(), envelope.GetAllAttributes()));
            }
            else
            {
                var action = new CommitActionEnlistment(() =>
                    {
                        queue.PutMessage(data);
                        SystemObserver.Notify(new EnvelopeSent(queue.Name, envelope.EnvelopeId, true,
                            envelope.Items.Select(x => x.MappedType.Name).ToArray(), envelope.GetAllAttributes()));
                    });
                Transaction.Current.EnlistVolatile(action, EnlistmentOptions.None);
            }
        }

        IQueueWriter GetOutboundQueue()
        {
            if (_queues.Length == 1)
                return _queues[0];
            var random = _random.Next(_queues.Length);
            return _queues[random];
        }

        sealed class CommitActionEnlistment : IEnlistmentNotification
        {
            readonly Action _commit;

            public CommitActionEnlistment(Action commit)
            {
                _commit = commit;
            }

            public void Prepare(PreparingEnlistment preparingEnlistment)
            {
                preparingEnlistment.Prepared();
            }

            public void Commit(Enlistment enlistment)
            {
                _commit();
                enlistment.Done();
            }

            public void Rollback(Enlistment enlistment)
            {
                enlistment.Done();
            }

            public void InDoubt(Enlistment enlistment)
            {
                enlistment.Done();
            }
        }

    }
}