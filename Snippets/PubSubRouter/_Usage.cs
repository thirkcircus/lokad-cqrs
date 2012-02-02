#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using Lokad.Cqrs;
using Lokad.Cqrs.Build;
using Lokad.Cqrs.Envelope;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace Snippets.PubSubRouter
{
    /// <summary>
    /// See ReadMe.markdown for details
    /// </summary>
    [TestFixture, Explicit]
    public sealed class _Usage
    {
        [Test]
        public void Test()
        {
            var types = Assembly.GetExecutingAssembly().GetExportedTypes()
                .Where(t => typeof(IPS_SampleMessage).IsAssignableFrom(t));
            var streamer = EnvelopeStreamer.CreateDefault(types);
            var builder = new CqrsEngineBuilder(streamer);
            // only message contracts within this class

            // configure in memory:
            //                            -> sub 1 
            //  inbox -> [PubSubRouter] <
            //                            -> sub 2
            //
            var store = new MemoryStorageConfig();
            var nuclear = store.CreateNuclear();

            var router = new PubSubRouter(nuclear, store.CreateWriteQueueFactory(), streamer);
            router.Init();

            builder.Dispatch(store.CreateInbox("sub1"), b => Console.WriteLine("sub1 hit"));
            builder.Dispatch(store.CreateInbox("sub2"), b => Console.WriteLine("sub2 hit"));
            builder.Handle(store.CreateInbox("inbox"), router.DispatchMessage);


            var sender = store.CreateSimpleSender(streamer, "inbox");

            using (var engine = builder.Build())
            using (var cts = new CancellationTokenSource())
            {
                var task = engine.Start(cts.Token);

                // no handler should get these.
                sender.SendOne(new SomethingHappened());
                sender.SendOne(new OtherHappened());

                // subscribe sub1 to all messages and sub2 to specific message
                sender.SendControl(eb =>
                    {
                        eb.AddString("router-subscribe:sub1", ".*");
                        eb.AddString("router-subscribe:sub2", "SomethingHappened");
                    });
                sender.SendOne(new SomethingHappened());
                sender.SendOne(new OtherHappened());

                // unsubscribe all
                sender.SendControl(eb =>
                    {
                        eb.AddString("router-unsubscribe:sub1", ".*");
                        eb.AddString("router-unsubscribe:sub2", "SomethingHappened");
                    });
                sender.SendOne(new SomethingHappened());
                sender.SendOne(new OtherHappened());


                task.Wait(5000);
            }
        }

        [DataContract]
        public sealed class SomethingHappened : IPS_SampleEvent {}

        [DataContract]
        public sealed class OtherHappened : IPS_SampleEvent {}
    }
}