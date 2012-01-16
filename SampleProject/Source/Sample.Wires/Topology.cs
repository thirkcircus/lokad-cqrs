#region Copyright (c) 2006-2012 LOKAD SAS. All rights reserved

// You must not remove this notice, or any other, from this software.
// This document is the property of LOKAD SAS and must not be disclosed

#endregion

using System;
using System.Linq;
using Lokad.Cqrs;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.Partition;
using Lokad.Cqrs.TapeStorage;

namespace Sample.Wires
{
    public static class Topology
    {
        public const string TimerQueue = "sample-queue-timer";
        public const string EventsQueue = "sample-queue-events";
        public const string EntityQueue = "sample-queue-entity";
        public const string DomainLogName = "domain.tmd";
        public const string ServiceQueue = "sample-queue-service";
        public const string RouterQueue = "sample-queue-router";

        public const string FutureMessagesContainer = "sample-future";
        public const string TapesContainer = "sample-tapes";

        public static Action<ImmutableEnvelope> Route(Func<string, IQueueWriter> factory, IEnvelopeStreamer serializer,
            ITapeStorageFactory tapes)
        {
            var events = factory(EventsQueue);
            var timerQueue = factory(TimerQueue);
            var entityQueue = factory(EntityQueue);
            var services = factory(ServiceQueue);
            var log = tapes.GetOrCreateStream(DomainLogName);
            return envelope =>
                {
                    var data = serializer.SaveEnvelopeData(envelope);
                    if (!log.TryAppend(data))
                        throw new InvalidOperationException("Failed to record domain log");

                    if (envelope.DeliverOnUtc > Current.UtcNow)
                    {
                        timerQueue.PutMessage(data);
                        return;
                    }
                    if (envelope.Items.All(i => i.Content is ICommand<IIdentity>))
                    {
                        entityQueue.PutMessage(data);
                        return;
                    }
                    if (envelope.Items.All(i => i.Content is IEvent<IIdentity>))
                    {
                        // we can have more than 1 entity event.
                        // all entity events are routed to events as separate
                        for (int i = 0; i < envelope.Items.Length; i++)
                        {
                            var name = envelope.EnvelopeId + "-e" + i;
                            var copy = EnvelopeBuilder.CloneProperties(name, envelope);
                            copy.AddItem(envelope.Items[i]);
                            events.PutMessage(serializer.SaveEnvelopeData(copy.Build()));
                        }
                        return;
                    }

                    if (envelope.Items.Length != 1)
                    {
                        throw new InvalidOperationException(
                            "Only entity commands or entity events can be batched");
                    }
                    var item = envelope.Items[0].Content;
                    if (item is IFunctionalCommand)
                    {
                        services.PutMessage(data);
                        return;
                    }
                    if (item is IFunctionalEvent || item is ISampleEvent)
                    {
                        events.PutMessage(data);
                        return;
                    }
                    throw new InvalidOperationException(string.Format("Unroutable message {0}", item));
                };
        }
    }
}