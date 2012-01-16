#region Copyright (c) 2006-2011 LOKAD SAS. All rights reserved

// You must not remove this notice, or any other, from this software.
// This document is the property of LOKAD SAS and must not be disclosed

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using Lokad.Cqrs;
using Lokad.Cqrs.AtomicStorage;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.Partition;
using Lokad.Cqrs.TapeStorage;
using Sample.Aggregates.Login;
using Sample.Aggregates.Security;

namespace Sample.Wires
{
    public sealed class AggregateFactory
    {
        readonly ITapeStorageFactory _factory;
        readonly IEnvelopeStreamer _streamer;
        readonly IQueueWriter _writer;
        readonly NuclearStorage _storage;
        readonly IIdentityGenerator _generator;

        public AggregateFactory(ITapeStorageFactory factory, IEnvelopeStreamer streamer, IQueueWriter writer, NuclearStorage storage, IIdentityGenerator generator)
        {
            _factory = factory;
            _streamer = streamer;
            _writer = writer;
            _storage = storage;
            _generator = generator;
        }

        public Applied Load(ICollection<ICommand<IIdentity>> commands)
        {
            var id = commands.First().Id;
            var stream = _factory.GetOrCreateStream(IdentityConvert.ToStream(id));
            var records = stream.ReadRecords(0, int.MaxValue).ToList();
            var events = records
                .SelectMany(r => _streamer.ReadAsEnvelopeData(r.Data).Items.Select(m => (IEvent<IIdentity>)m.Content))
                .ToArray();

            var then = new Applied();

            if (records.Count > 0)
            {
                then.Version = records.Last().Version;
            }
    
            var user = id as UserId;
            if (user != null)
            {
                var state = new UserAggregateState(events);
                var agg = new UserAggregate(state, then.Events.Add);
                ExecuteSafely(agg,commands);
                return then;
            }

            var security = id as SecurityId;

            if (security != null)
            {
                var state = new SecurityAggregateState(events);
                var agg = new SecurityAggregate(state, then.Events.Add, new PasswordGenerator(), _generator);
                ExecuteSafely(agg, commands);
                return then;
            }

            throw new NotSupportedException("identity not supported " + id);
        }


        public static void ExecuteSafely<TIdentity>(IAggregate<TIdentity> self, IEnumerable<ISampleCommand> commands)
            where TIdentity : IIdentity
        {
            foreach (var hubCommand in commands)
            {
                self.Execute((ICommand<TIdentity>) hubCommand);
            }
        }

        public sealed class Applied
        {
            public List<IEvent<IIdentity>> Events = new List<IEvent<IIdentity>>();
            public long Version;
        }

        public void Dispatch(ImmutableEnvelope e)
        {
            var commands = e.Items.Select(i => (ICommand<IIdentity>)i.Content).ToList();
            var id = commands.First().Id;
            var builder = new StringBuilder();
            var old = Context.SwapFor(s => builder.AppendLine(s));
            Applied results;
            try
            {
                results = Load(commands);
            }
            finally
            {
                Context.SwapFor(old);
            }

            var s1 = builder.ToString();
            if (!String.IsNullOrEmpty(s1))
            {
                Context.Debug(s1.TrimEnd('\r', '\n'));
            }
            AppendToStream(id, e.EnvelopeId, results, s1);
            PublishEvents(id, results);
        }

        void PublishEvents(IIdentity id, Applied then)
        {
            var arVersion = then.Version + 1;
            var arName = IdentityConvert.ToTransportable(id);
            var name = String.Format("{0}-{1}", arName, arVersion);
            var builder = new EnvelopeBuilder(name);
            builder.AddString("entity", arName);

            foreach (var @event in then.Events)
            {
                builder.AddItem((object)@event);
            }
            _writer.PutMessage(_streamer.SaveEnvelopeData(builder.Build()));
        }

        void AppendToStream(IIdentity id, string envelopeId, Applied then, string explanation)
        {
            var stream = _factory.GetOrCreateStream(IdentityConvert.ToStream(id));
            var b = new EnvelopeBuilder("unknown");
            b.AddString("caused-by", envelopeId);

            if (!String.IsNullOrEmpty(explanation))
            {
                b.AddString("explain", explanation);
            }
            foreach (var e in then.Events)
            {
                b.AddItem((object)e);
            }
            var data = _streamer.SaveEnvelopeData(b.Build());
            Context.Debug("?? Append {0} at v{3} to '{1}' in thread {2}", then.Events.Count,
                IdentityConvert.ToStream(id),
                Thread.CurrentThread.ManagedThreadId,
                then.Version);

            if (!stream.TryAppend(data, TapeAppendCondition.VersionIs(then.Version)))
            {
                throw new InvalidOperationException("Failed to update the stream - it has been changed concurrently");
            }
        }
    }
}