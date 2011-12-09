using System;
using Lokad.Cqrs.Core.Inbox;

namespace Lokad.Cqrs.Core.Dispatch.Events
{
    [Serializable]
    public sealed class DispatchRecoveryFailed : ISystemEvent
    {
        public Exception DispatchException { get; private set; }
        public EnvelopeTransportContext Envelope { get; private set; }
        public string QueueName { get; private set; }

        public DispatchRecoveryFailed(Exception exception, EnvelopeTransportContext envelope, string queueName)
        {
            DispatchException = exception;
            Envelope = envelope;
            QueueName = queueName;
        }

        public override string ToString()
        {
            return string.Format("Failed to recover dispatch '{0}' from '{1}': {2}", Envelope.EnvelopeId, QueueName, DispatchException.Message);
        }
    }
}