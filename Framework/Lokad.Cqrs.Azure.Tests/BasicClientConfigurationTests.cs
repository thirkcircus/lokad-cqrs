using System.Runtime.Serialization;
using System.Threading;
using Lokad.Cqrs.Build.Client;
using Lokad.Cqrs.Build.Engine;
using Lokad.Cqrs.Core.Dispatch.Events;
using NUnit.Framework;
using System.Linq;

namespace Lokad.Cqrs
{
    [TestFixture]
    public sealed class BasicClientConfigurationTests
    {
        [DataContract]
        public sealed class Message : Define.Command
        {
            
        }

        
        
        // ReSharper disable InconsistentNaming
        [Test]
        public void Test()
        {
            var dev = AzureStorage.CreateConfigurationForDev();
            WipeAzureAccount.Fast(s => s.StartsWith("test-"), dev);

            var b = new CqrsEngineBuilder();
            b.Azure(c => c.AddAzureProcess(dev, "test-publish",HandlerComposer.Empty));
            using (var source = new CancellationTokenSource())
            using (b.When<MessageAcked>(e => source.Cancel()))
            using (var engine = b.Build())
            {
                var task = engine.Start(source.Token);

                var builder = new CqrsClientBuilder();
                builder.Azure(c => c.AddAzureSender(dev, "test-publish"));
                var client = builder.Build();


                client.Sender.SendOne(new Message());
                if (!task.Wait(5000))
                {
                    source.Cancel();
                }
            }
        }
    }
}