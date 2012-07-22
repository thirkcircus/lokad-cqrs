using System;
using System.Collections.Generic;
using System.Linq;
using Lokad.Cqrs;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.Partition;
using Lokad.Cqrs.TapeStorage;

namespace SaaS.Wires
{
    public sealed class TapeStreamEventStore : IEventStore
    {
        public TapeStreamEventStore(ITapeContainer factory, IEnvelopeStreamer streamer, IQueueWriter writer)
        {
            _factory = factory;
            _streamer = streamer;
            _writer = writer;
        }

        readonly ITapeContainer _factory;
        readonly IEnvelopeStreamer _streamer;
        readonly IQueueWriter _writer;

        public EventStream LoadEventStream(IIdentity id)
        {
            var partitionedName = IdentityConvert.ToStream(id);
            var stream = _factory.GetOrCreateStream(partitionedName);
            var records = stream.ReadRecords(0, int.MaxValue).ToList();
            var events = records
                .SelectMany(r => _streamer.ReadAsEnvelopeData(r.Data).Items.Select(m => (IEvent)m.Content))
                .ToArray();
            var version = 0L;
            if (records.Count > 0)
            {
                version = records.Last().Version;
            }
            return new EventStream
                {
                    Events = events.ToList(),
                    Version = version
                };
        }

        public void AppendToStream(IIdentity id, long originalVersion, ICollection<IEvent> events)
        {

            if (events.Count == 0)
                return;
            var stream = _factory.GetOrCreateStream(IdentityConvert.ToStream(id));
            var b = new EnvelopeBuilder("unknown");

            foreach (var e in events)
            {
                b.AddItem((object)e);
            }
            var data = _streamer.SaveEnvelopeData(b.Build());

            var result = stream.TryAppend(data, TapeAppendCondition.VersionIs(originalVersion));
            if (result == 0)
            {
                // this is temporary implementation
                throw OptimisticConcurrencyException.Create(-1, originalVersion, id, null);
            }

            PublishDomainSuccess(id, events, originalVersion);
        }

        void PublishDomainSuccess(IIdentity id, IEnumerable<IEvent> events, long version)
        {
            var arVersion = version + 1;
            var arName = IdentityConvert.ToTransportable(id);
            var name = String.Format("{0}-{1}", arName, arVersion);
            var builder = new EnvelopeBuilder(name);
            builder.AddString("entity", arName);

            foreach (var @event in events)
            {
                builder.AddItem((object)@event);
            }

            _writer.PutMessage(_streamer.SaveEnvelopeData(builder.Build()));
        }
    }
}