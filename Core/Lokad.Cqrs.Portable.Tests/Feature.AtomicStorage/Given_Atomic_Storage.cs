using Lokad.Cqrs.AtomicStorage;
using NUnit.Framework;

namespace Lokad.Cqrs.Feature.AtomicStorage
{
    public abstract class Given_Atomic_Storage
    {
        protected abstract NuclearStorage Compose(IAtomicStorageStrategy strategy);
        

        [Test]
        public void SimpleWriteRoundtrip()
        {
            var strategy = new DefaultAtomicStorageStrategyBuilder().Build();
            var setup = Compose(strategy);

            setup.AddOrUpdateEntity(1, "test");
            setup.AddOrUpdateSingleton(() => 1, i => 1);

            AssertContents(setup);

            var mem = new MemoryStorageConfig().CreateNuclear(strategy);
            mem.CopyFrom(setup);

            AssertContents(mem);

            setup.Reset();

            setup.CopyFrom(mem);

            AssertContents(setup);
        }

        private static void AssertContents(NuclearStorage setup)
        {
            Assert.AreEqual("test", setup.GetEntity<string>(1).Value);
            Assert.AreEqual(1, setup.GetSingleton<int>().Value);
        }
    }
}