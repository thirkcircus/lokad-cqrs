using System;

namespace Lokad.Cqrs.Envelope.Events
{
    [Serializable]
    public sealed class EnvelopeDuplicateDiscarded : ISystemEvent
    {
        public string EnvelopeId { get; private set; }

        public EnvelopeDuplicateDiscarded(string envelopeId)
        {
            EnvelopeId = envelopeId;
        }

        public override string ToString()
        {
            return string.Format("[{0}] duplicate discarded", EnvelopeId);
        }
    }
}