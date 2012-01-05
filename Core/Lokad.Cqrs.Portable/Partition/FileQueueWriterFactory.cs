using System.IO;

namespace Lokad.Cqrs.Partition
{
    public sealed class FileQueueWriterFactory : IQueueWriterFactory
    {
        readonly FileStorageConfig _account;
        readonly string _endpoint;

        public FileQueueWriterFactory(FileStorageConfig account)
        {
            _account = account;
            _endpoint = _account.AccountName;
        }

        public string Endpoint
        {
            get { return _endpoint; }
        }

        public IQueueWriter GetWriteQueue(string queueName)
        {
            var full = Path.Combine(_account.Folder.FullName, queueName);
            if (!Directory.Exists(full))
            {
                Directory.CreateDirectory(full);
            }
            return
                new FileQueueWriter(new DirectoryInfo(full), queueName);
        }
    }
}