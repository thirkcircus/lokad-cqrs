using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading;
using Lokad.Cqrs;
using Lokad.Cqrs.Build.Engine;
using Lokad.Cqrs.Core;
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
            var builder = new CqrsEngineBuilder();
            // only message contracts within this class
            builder.Messages(m => m.WhereMessages(t => t== typeof(SecondPassed)));


            var storage = FileStorage.CreateConfig(GetType().Name, "fb");
            storage.Reset();
            Console.WriteLine(storage.FullPath);
            builder.File(module =>
                {
                    module.AddFileSender(storage, "inbox", x => x.IdGeneratorForTests());
                    module.AddFileTimer(storage, "timer", "inbox");
                    module.AddFileProcess(storage, "process", c => (envelope => Console.WriteLine(envelope.DeliverOnUtc)));
                    module.AddFileRouter(storage, "inbox", OnConfig);
                });

            using (var engine = builder.Build())
            {
                engine.Resolve<IMessageSender>().SendOne(new SecondPassed(), c => c.DelayBy(TimeSpan.FromSeconds(4)));
                engine.RunForever();
            }
        }

        string OnConfig(ImmutableEnvelope cb)
        {
            Console.WriteLine("Routed");
            return cb.DeliverOnUtc < DateTime.UtcNow ? "fb:process" : "fb:timer";
        }

        // ReSharper disable InconsistentNaming

        static void WhenSecondPassed(SecondPassed p)
        {
            Trace.WriteLine("yet another second passed");
        }

        static Action<ImmutableEnvelope> Bootstrap(Container container)
        {
            var composer = new HandlerComposer();

            composer.Add<SecondPassed>(WhenSecondPassed);

            var action = composer.BuildHandler(container);


            return action;
        }
    }
    [DataContract]
    public sealed class SecondPassed
    {
        
    }
}