using System;
using System.IO;
using System.Text;
using Lokad.Cqrs.Build.Engine;
using NUnit.Framework;

namespace Lokad.Cqrs
{
    [TestFixture]
    public sealed class MiscTests
    {
        // ReSharper disable InconsistentNaming
        [Test]
        public void Azure_queues_regex_is_valid()
        {
            Assert.IsTrue(AzureEngineModule.QueueName.IsMatch("some-queue"));
            Assert.IsTrue(AzureEngineModule.QueueName.IsMatch("to-watchtower"));
            Assert.IsFalse(AzureEngineModule.QueueName.IsMatch("-some-queue"));
        }

        [Test, Explicit]
        public void MeasureSize()
        {
            using (var m = new MemoryStream())
            {
                while (true)
                {
                    m.WriteByte(1);
                    var i = Encoding.ASCII.GetByteCount(Convert.ToBase64String(m.ToArray()));
                    if (i > 1024*64)
                    {
                        Console.WriteLine(m.Length-1);
                        return;
                    }
                }
            }
        }
    }
}