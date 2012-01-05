#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;
using System.Runtime.Serialization;
using Lokad.Cqrs;
using Lokad.Cqrs.Build;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.TimerService;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace Snippets.SimpleTimerService
{
    [TestFixture, Explicit]
    public sealed class SimpleTimerService_Usage
    {
        [Test]
        public void Test()
        {
            var streamer = EnvelopeStreamer.CreateDefault(typeof(SecondPassed));
            var builder = new CqrsEngineBuilder(streamer);
            var store = FileStorage.CreateConfig(GetType().Name, reset:true);


            builder.Handle(store.CreateInbox("inbox"), BuildRouter(store, streamer));
            builder.Handle(store.CreateInbox("process"), ie => Console.WriteLine("Message from past!"));

            var inboxWriter = store.CreateQueueWriter("inbox");
            var futureContainer = store.CreateStreaming().GetContainer("future");
            var timer = new StreamingTimerService(inboxWriter, futureContainer, streamer);

            builder.AddProcess(timer);
            builder.Handle(store.CreateInbox("timer"), timer.PutMessage);

            using (var engine = builder.Build())
            {
                var bytes = streamer.SaveEnvelopeData(new SecondPassed(), c => c.DelayBy(TimeSpan.FromSeconds(4)));
                inboxWriter.PutMessage(bytes);
                Console.WriteLine("Sent message to future...");
                engine.RunForever();
            }
        }

        static Action<ImmutableEnvelope> BuildRouter(FileStorageConfig storage, IEnvelopeStreamer streamer)
        {
            return envelope =>
                {
                    var target = (envelope.DeliverOnUtc < DateTime.UtcNow ? "process" : "timer");
                    Console.WriteLine("Routed to " + target);
                    storage.CreateQueueWriter(target).PutMessage(streamer.SaveEnvelopeData(envelope));
                };
        }
    }

    [DataContract]
    public sealed class SecondPassed {}
}