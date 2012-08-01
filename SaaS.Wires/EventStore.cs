using System;
using System.Collections.Generic;
using System.Linq;
using Lokad.Cqrs;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.Partition;
using Lokad.Cqrs.TapeStorage;

namespace SaaS.Wires
{
    public sealed class EventStore : IEventStore
    {
        public EventStore(IAppendOnlyStore appendOnlyStore, IEnvelopeStreamer streamer, IQueueWriter writer)
        {
            _appendOnlyStore = appendOnlyStore;
            _streamer = streamer;
            _writer = writer;
        }

        readonly IAppendOnlyStore _appendOnlyStore;
        readonly IEnvelopeStreamer _streamer;
        readonly IQueueWriter _writer;

        public EventStream LoadEventStream(IIdentity id)
        {
            return LoadEventStream(id, 0, int.MaxValue);
        }

        public EventStream LoadEventStream(IIdentity id, long skipEvents, int maxCount)
        {
            var name = IdentityToString(id);
            var records = _appendOnlyStore.ReadRecords(name, skipEvents, maxCount).ToList();

            var stream = new EventStream();
            foreach (var tapeRecord in records)
            {
                stream.Events.AddRange(DeserializeEvent(tapeRecord.Data));
                stream.Version = tapeRecord.Version;
            }
            return stream;
        }

        IEnumerable<IEvent> DeserializeEvent(byte[] data)
        {
            return _streamer.ReadAsEnvelopeData(data).Items.Select(i => (IEvent)i.Content);
        }

        public static string IdentityToString(IIdentity identity)
        {
            return identity.GetId();
        }

        public void AppendToStream(IIdentity id, long originalVersion, ICollection<IEvent> events)
        {
            if (events.Count == 0)
                return;

            var name = IdentityToString(id);
            var data = SerializeEvents(events);
            try
            {
                _appendOnlyStore.Append(name, data, originalVersion);
            }
            catch (AppendOnlyStoreConcurrencyException e)
            {
                // load server events
                var server = LoadEventStream(id);
                // throw a real problem
                throw OptimisticConcurrencyException.Create(server.Version, e.ExpectedStreamVersion, id, server.Events);
            }
            PublishDomainSuccess(id, events, originalVersion);
        }

        byte[] SerializeEvents(IEnumerable<IEvent> events)
        {
            var b = new EnvelopeBuilder("unknown");
            foreach (var e in events)
            {
                b.AddItem((object)e);
            }
            var data = _streamer.SaveEnvelopeData(b.Build());
            return data;
        }

        void PublishDomainSuccess(IIdentity id, IEnumerable<IEvent> events, long version)
        {
            var arVersion = version + 1;
            var arName = id.GetTag() + "-" + id.GetId();
            var name = String.Format("{0}-{1}", arName, arVersion);
            var builder = new EnvelopeBuilder(name);

            foreach (var @event in events)
            {
                builder.AddItem((object)@event);
            }

            _writer.PutMessage(_streamer.SaveEnvelopeData(builder.Build()));
        }
    }
}