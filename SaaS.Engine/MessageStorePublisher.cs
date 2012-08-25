using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using Lokad.Cqrs;
using Lokad.Cqrs.AtomicStorage;

namespace SaaS.Wires
{
    /// <summary>
    /// Is responsible for publishing events from the event store
    /// </summary>
    public sealed class MessageStorePublisher
    {
        readonly MessageStore _store;
        readonly MessageSender _sender;
        readonly NuclearStorage _storage;

        public MessageStorePublisher(MessageStore store, MessageSender sender, NuclearStorage storage)
        {
            _store = store;
            _sender = sender;
            _storage = storage;
        }

        public sealed class PublishResult
        {
            public readonly long InitialVersion;
            public readonly long FinalVersion;
            public readonly int BatchSize;

            public readonly bool Changed;
            public readonly bool HasMoreWork;

            public PublishResult(long initialVersion, long finalVersion, int batchSize)
            {
                InitialVersion = initialVersion;
                FinalVersion = finalVersion;
                BatchSize = batchSize;

                Changed = InitialVersion != FinalVersion;
                HasMoreWork = (FinalVersion - InitialVersion) < BatchSize;
            }
        }

        PublishResult PublishEventsIfAnyNew(long initialPosition, int count)
        {
            var records = _store.EnumerateAllItems(initialPosition, count);
            var currentPosition = initialPosition;
            int evts = 0;
            foreach (var e in records)
            {
                if (e.StoreVersion <= currentPosition)
                {
                    throw new InvalidOperationException("Retrieved record with wrong position");
                }
                if (e.Key != "audit")
                {
                    for (int i = 0; i < e.Items.Length; i++)
                    {
                        // predetermined id to kick in event deduplication
                        // if server crashes somehow
                        var envelopeId = "esp-" + e.StoreVersion + "-" + i;
                        var item = e.Items[i];

                        evts += 1;
                        _sender.Send(item, envelopeId);
                    }
                }
                currentPosition = e.StoreVersion;
            }
            var result = new PublishResult(initialPosition, currentPosition, count);
            if (result.Changed)
            {
                SystemObserver.Notify("[sys ] ES marker moved to {0} ({1} events published)", result.FinalVersion, evts);
            }
            return result;
        }

        public void Run(CancellationToken token)
        {
            long? currentPosition = null;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // reinitialize state from persistent store, if absent
                    if (currentPosition == null)
                    {
                        // if we fail here, we'll get into retry
                        currentPosition = _storage.GetSingletonOrNew<PublishCounter>().Position;
                    }
                    // publish events, if any
                    var publishResult = PublishEventsIfAnyNew(currentPosition.Value, 25);
                    if (publishResult.Changed)
                    {
                        // ok, we are changed, persist that to survive crashes
                        var output = _storage.UpdateSingletonEnforcingNew<PublishCounter>(c =>
                            {
                                if (c.Position != publishResult.InitialVersion)
                                {
                                    throw new InvalidOperationException("Somebody wrote in parallel. Blow up!");
                                }
                                // we are good - update ES
                                c.Position = publishResult.FinalVersion;

                            });
                        currentPosition = output.Position;
                    }
                    if (!publishResult.HasMoreWork)
                    {
                        // wait for a few ms before polling ES again
                        token.WaitHandle.WaitOne(400);
                    }
                }
                catch (Exception ex)
                {
                    // we messed up, roll back
                    currentPosition = null;
                    Trace.WriteLine(ex);
                    token.WaitHandle.WaitOne(5000);
                }
            }
        }

        public void VerifyEventStreamSanity()
        {
            var result = _storage.GetSingletonOrNew<PublishCounter>();
            if (result.Position != 0)
            {
                SystemObserver.Notify("Continuing work with existing event store");
                return;
            }
            var store = _store.EnumerateAllItems(0, 100).ToArray();
            if (store.Length == 0)
            {
                SystemObserver.Notify("Opening new event stream");
                _sender.SendHashed(new EventStreamStarted());
                return;
            }
            if (store.Length == 100)
            {
                throw new InvalidOperationException(
                    "It looks like event stream really went ahead. Do you mean to resend all events?");
            }
        }

        [DataContract]
        public sealed class PublishCounter
        {
            [DataMember(Order = 1)]
            public long Position { get; set; }
        }
    }
}