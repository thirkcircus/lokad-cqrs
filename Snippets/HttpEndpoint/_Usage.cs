#region (c) 2010-2011 Lokad CQRS - New BSD License

// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;
using System.Diagnostics;
using System.Linq;
using Lokad.Cqrs;
using Lokad.Cqrs.AtomicStorage;
using Lokad.Cqrs.Build;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.Feature.Http;
using Lokad.Cqrs.Feature.Http.Handlers;
using NUnit.Framework;
using Snippets.HttpEndpoint.View;

// ReSharper disable InconsistentNaming

namespace Snippets.HttpEndpoint
{
    [TestFixture]
    public sealed class _Usage
    {
        [Test]
        [Explicit("Run manually and browse url http://localhost:8082/index.htm to see results.")]
        public void Test()
        {
            // this test will start a simple web server on port 8082 (with a CQRS engine)
            // You might need to reserve that port or run test as admin. Check out unit test
            // output for the exact command line on port reservation (or MSDN docs)
            //
            // netsh http add urlacl url=http://+:8082/ user=RINAT-R5\Rinat.Abdullin
            // after starting the test, navigate you browser to localhost:8082/index.htm
            // and try dragging the image around



            // in-memory structure to capture mouse movement
            // statistics
            var stats = new MouseStats();


            // we accept a message just of this type, using a serializer
            var messages = new[] { typeof(MouseMoved), typeof(MouseClick) };
            var serializer = new MyJsonSerializer(messages);
            var streamer = new EnvelopeStreamer(serializer);
            var store = new MemoryStorageConfig();
            var atomic = store.CreateNuclear().Factory;

            // let's configure our custom Http server to 
            // 1. serve resources
            // 2. serve MouseStats View
            // 3. accept commands
            var environment = new HttpEnvironment { Port = 8082 };
            var builder = new CqrsEngineBuilder(streamer);

            builder.AddProcess(new Listener(environment,
                 new EmbeddedResourceHttpRequestHandler(typeof(MouseMoved).Assembly, "Snippets.HttpEndpoint"),
                 new MouseStatsRequestHandler(stats),
                 new HeatMapRequestHandler(atomic.GetEntityReader<unit, HeatMapView>()),
                 new MouseEventsRequestHandler(store.CreateQueueWriter("inbox"), serializer, streamer)));

            builder.Handle(store.CreateInbox("inbox"), envelope =>
                {
                    if (envelope.Items.Any(i => i.Content is MouseMoved))
                    {
                        MouseStatHandler(envelope, stats);
                    }
                    else if (envelope.Items.Any(i => i.Content is MouseClick))
                    {
                        MouseClickHandler(envelope, atomic.GetEntityWriter<unit, PointsView>());
                    }
                });


            builder.AddProcess(new HeatMapGenerateTask(atomic.GetEntityReader<unit, PointsView>(), atomic.GetEntityWriter<unit, HeatMapView>()));
   

         

            Process.Start("http://localhost:8082/index.htm");
            // this is a test, so let's block everything
            builder.Build().RunForever();
        }

        private static void MouseStatHandler(ImmutableEnvelope envelope, MouseStats stats)
        {
            var mouseMovedEvent = (MouseMoved)envelope.Items[0].Content;

            stats.MessagesCount++;

            stats.Distance += (long)Math.Sqrt(Math.Pow(mouseMovedEvent.X1 - mouseMovedEvent.X2, 2)
                                  + Math.Pow(mouseMovedEvent.Y1 - mouseMovedEvent.Y2, 2));
            stats.RecordMessage();
        }

        private static void MouseClickHandler(ImmutableEnvelope envelope, IAtomicWriter<unit, PointsView> writer)
        {
            var mouseMovedEvent = (MouseClick)envelope.Items[0].Content;

            writer.AddOrUpdate(unit.it, () => new PointsView(),
                v =>
                {
                    var Point = v.Points.FirstOrDefault(p => p.X == mouseMovedEvent.X && p.Y == mouseMovedEvent.Y);
                    if (Point != null)
                    {
                        Point.Intensity += 10;
                    }
                    else
                    {
                        v.Points.Add(new HeatPoint(mouseMovedEvent.X, mouseMovedEvent.Y, 50));
                    }
                });
        }
    }
}