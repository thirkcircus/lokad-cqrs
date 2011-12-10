using System;
using Lokad.Cqrs.Core.Inbox;

namespace Lokad.Cqrs.Core.Dispatch.Events
{
    [Serializable]
    public sealed class EnvelopeQuarantined : ISystemEvent
    {
        public Exception LastException { get; private set; }
        public ImmutableEnvelope Envelope { get; private set; }


        public EnvelopeQuarantined(Exception lastException, ImmutableEnvelope envelope)
        {
            LastException = lastException;
            Envelope = envelope;
        }

        public override string ToString()
        {
            return string.Format("Quarantined '{0}': {1}", Envelope.EnvelopeId, LastException.Message);
        }
    }

    [Serializable]
    public sealed class EnvelopeCleanupFailed : ISystemEvent
    {
        public Exception Exception { get; private set; }
        public ImmutableEnvelope Envelope { get; private set; }

        public EnvelopeCleanupFailed(Exception exception, ImmutableEnvelope envelope)
        {
            Exception = exception;
            Envelope = envelope;
        }
    }

    [Serializable]
    public sealed class EnvelopeDispatched : ISystemEvent
    {
        public ImmutableEnvelope Envelope { get; private set; }

        public EnvelopeDispatched(ImmutableEnvelope envelope)
        {
            Envelope = envelope;
        }
    }
}