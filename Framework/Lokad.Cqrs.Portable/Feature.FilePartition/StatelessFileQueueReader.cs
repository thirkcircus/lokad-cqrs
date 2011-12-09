using System;
using System.IO;
using System.Linq;
using Lokad.Cqrs.Core.Dispatch.Events;
using Lokad.Cqrs.Core.Inbox;
using Lokad.Cqrs.Core.Inbox.Events;

namespace Lokad.Cqrs.Feature.FilePartition
{
    public sealed class StatelessFileQueueReader
    {
        readonly ISystemObserver _observer;

        readonly DirectoryInfo _queue;
        readonly string _queueName;

        public string Name
        {
            get { return _queueName; }
        }

        public StatelessFileQueueReader(ISystemObserver observer, DirectoryInfo queue, string queueName)
        {
            _observer = observer;
            _queue = queue;
            _queueName = queueName;
        }

        public GetEnvelopeResult TryGetMessage()
        {
            FileInfo message;
            try
            {
                message = _queue.EnumerateFiles().FirstOrDefault();
            }
            catch (Exception ex)
            {
                _observer.Notify(new FailedToReadMessage(ex, _queueName));
                return GetEnvelopeResult.Error();
            }

            if (null == message)
            {
                return GetEnvelopeResult.Empty;
            }

            try
            {
                var buffer = File.ReadAllBytes(message.FullName);

                var unpacked = new MessageTransportContext(message, buffer, _queueName);
                return GetEnvelopeResult.Success(unpacked);
            }
            catch (IOException ex)
            {
                // this is probably sharing violation, no need to 
                // scare people.
                if (!IsSharingViolation(ex))
                {
                    _observer.Notify(new FailedToAccessStorage(ex, _queue.Name, message.Name));
                }
                return GetEnvelopeResult.Retry;
            }
            catch (Exception ex)
            {
                _observer.Notify(new MessageInboxFailed(ex, _queue.Name,message.FullName));
                // new poison details
                return GetEnvelopeResult.Retry;
            }
        }

        static bool IsSharingViolation(IOException ex)
        {
            // http://stackoverflow.com/questions/425956/how-do-i-determine-if-an-ioexception-is-thrown-because-of-a-sharing-violation
            // don't ask...
            var hResult = System.Runtime.InteropServices.Marshal.GetHRForException(ex);
            const int sharingViolation = 32;
            return (hResult & 0xFFFF) == sharingViolation;
        }

        public void Initialize()
        {
            _queue.Create();
        }

        /// <summary>
        /// ACKs the message by deleting it from the queue.
        /// </summary>
        /// <param name="message">The message context to ACK.</param>
        public void AckMessage(MessageTransportContext message)
        {
            if (message == null) throw new ArgumentNullException("message");
            ((FileInfo)message.TransportMessage).Delete();
        }
    }
}