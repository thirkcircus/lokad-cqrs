namespace Lokad.Cqrs.Partition
{
    public sealed class MemoryQueueWriterFactory :IQueueWriterFactory
    {
        readonly MemoryAccount _account;
        readonly string _endpoint;

        public MemoryQueueWriterFactory(MemoryAccount account, string endpoint = "memory")
        {
            _account = account;
            _endpoint = endpoint;
        }

        public string Endpoint
        {
            get { return _endpoint; }
        }

        public IQueueWriter GetWriteQueue(string queueName)
        {
            return _account.CreateQueueWriter(queueName);
        }
    }
}