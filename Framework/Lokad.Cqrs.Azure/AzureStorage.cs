using System;
using Lokad.Cqrs.Build;
using Lokad.Cqrs.Core.Reactive;
using Lokad.Cqrs.Feature.AtomicStorage;
using Lokad.Cqrs.Feature.StreamingStorage;
using Lokad.Cqrs.Feature.TapeStorage;
using Microsoft.WindowsAzure;
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Lokad.Cqrs
{

    /// <summary>
    /// Helper class to access Azure storage outside of the engine, if needed
    /// </summary>
    public static class AzureStorage
    {



        /// <summary>
        /// Creates the simplified nuclear storage wrapper around Atomic storage, using the default storage
        /// strategy.
        /// </summary>
        /// <param name="storageConfig">The storage config.</param>
        /// <returns>new instance of the nuclear storage</returns>
        public static NuclearStorage CreateNuclear(this IAzureStorageConfig storageConfig)
        {
            return CreateNuclear(storageConfig, b => { });
        }

        /// <summary> Creates the simplified nuclear storage wrapper around Atomic storage. </summary>
        /// <param name="storageConfig">The storage config.</param>
        /// <param name="strategy">The atomic storage strategy.</param>
        /// <returns></returns>
        public static NuclearStorage CreateNuclear(this IAzureStorageConfig storageConfig, IAtomicStorageStrategy strategy)
        {
            var factory = new AzureAtomicStorageFactory(strategy, storageConfig);
            return new NuclearStorage(factory);
        }

        /// <summary> Creates the simplified nuclear storage wrapper around Atomic storage. </summary>
        /// <param name="storageConfig">The storage config.</param>
        /// <param name="configStrategy">The config strategy.</param>
        /// <returns></returns>
        public static NuclearStorage CreateNuclear(this IAzureStorageConfig storageConfig, Action<DefaultAtomicStorageStrategyBuilder> configStrategy)
        {
            var strategyBuilder = new DefaultAtomicStorageStrategyBuilder();
            configStrategy(strategyBuilder);
            var strategy = strategyBuilder.Build();
            return CreateNuclear(storageConfig, strategy);
        }

        /// <summary> Creates the storage access configuration. </summary>
        /// <param name="cloudStorageAccount">The cloud storage account.</param>
        /// <param name="storageConfigurationStorage">The config storage.</param>
        /// <returns></returns>
        public static IAzureStorageConfig CreateConfig(CloudStorageAccount cloudStorageAccount, Action<AzureStorageConfigurationBuilder> storageConfigurationStorage)
        {
            var builder = new AzureStorageConfigurationBuilder(cloudStorageAccount);
            storageConfigurationStorage(builder);

            return builder.Build();
        }

        /// <summary>
        /// Creates the storage access configuration.
        /// </summary>
        /// <param name="storageString">The storage string.</param>
        /// <param name="storageConfiguration">The storage configuration.</param>
        /// <returns></returns>
        public static IAzureStorageConfig CreateConfig(string storageString, Action<AzureStorageConfigurationBuilder> storageConfiguration)
        {
            return CreateConfig(CloudStorageAccount.Parse(storageString), storageConfiguration);
        }

        /// Creates the storage access configuration.
        /// <param name="storageString">The storage string.</param>
        /// <returns></returns>
        public static IAzureStorageConfig CreateConfig(string storageString)
        {
            return CreateConfig(storageString, builder => { });
        }

        /// <summary>
        /// Creates the storage access configuration for the development storage emulator.
        /// </summary>
        /// <returns></returns>
        public static IAzureStorageConfig CreateConfigurationForDev()
        {
            return CreateConfig(CloudStorageAccount.DevelopmentStorageAccount, c => c.Named("azure-dev"));
        }

        /// <summary>
        /// Creates the storage access configuration.
        /// </summary>
        /// <param name="cloudStorageAccount">The cloud storage account.</param>
        /// <returns></returns>
        public static IAzureStorageConfig CreateConfig(CloudStorageAccount cloudStorageAccount)
        {
            return CreateConfig(cloudStorageAccount, builder => { });
        }

        /// <summary>
        /// Creates the streaming storage out of the provided storage config.
        /// </summary>
        /// <param name="config">The config.</param>
        /// <returns></returns>
        public static IStreamingRoot CreateStreaming(this IAzureStorageConfig config)
        {
            return new BlobStreamingRoot(config.CreateBlobClient());
        }

  
        public static IStreamingContainer CreateStreaming(this IAzureStorageConfig config, string container)
        {
            return config.CreateStreaming().GetContainer(container).Create();
        }

        /// <summary>
        /// Creates the tape storage factory for windows Azure storage.
        /// </summary>
        /// <param name="config">Azure storage configuration to create tape storage with.</param>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="initializeForWriting">if set to <c>true</c>, then storage is initialized for writing as needed.</param>
        /// <returns></returns>
        public static BlobTapeStorageFactory CreateTape(this IAzureStorageConfig config, string containerName = "tapes")
        {
            var factory = new BlobTapeStorageFactory(config, containerName);
            factory.InitializeForWriting();
            return factory;
        }

    }
}