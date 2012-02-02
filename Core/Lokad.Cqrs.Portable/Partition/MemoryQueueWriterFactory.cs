namespace Lokad.Cqrs.Partition
{
    public sealed class MemoryQueueWriterFactory :IQueueWriterFactory
    {
        readonly MemoryStorageConfig _storageConfig;
        readonly string _endpoint;

        public MemoryQueueWriterFactory(MemoryStorageConfig storageConfig, string endpoint = "memory")
        {
            _storageConfig = storageConfig;
            _endpoint = endpoint;
        }

        public string Endpoint
        {
            get { return _endpoint; }
        }

        public IQueueWriter GetWriteQueue(string queueName)
        {
            return _storageConfig.CreateQueueWriter(queueName);
        }
    }
}