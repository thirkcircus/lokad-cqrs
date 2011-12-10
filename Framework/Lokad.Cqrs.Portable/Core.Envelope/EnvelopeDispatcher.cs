using System;
using System.Threading;
using Lokad.Cqrs.Core.Dispatch;
using Lokad.Cqrs.Core.Dispatch.Events;
using Lokad.Cqrs.Core.Inbox.Events;

namespace Lokad.Cqrs.Core.Envelope
{
    public sealed class EnvelopeDispatcher
    {
        readonly Action<ImmutableEnvelope> _action;
        readonly IEnvelopeQuarantine _quarantine;
        readonly MessageDuplicationMemory _manager;
        readonly IEnvelopeStreamer _streamer;
        readonly ISystemObserver _observer;

        public EnvelopeDispatcher(Action<ImmutableEnvelope> action, IEnvelopeQuarantine quarantine, MessageDuplicationManager manager, IEnvelopeStreamer streamer, ISystemObserver observer)
        {
            _action = action;
            _quarantine = quarantine;
            _manager = manager.GetOrAdd(this);
            _streamer = streamer;
            _observer = observer;
        }


        public void Dispatch(byte[] message)
        {

            ImmutableEnvelope envelope = null;
            try
            {
                envelope = _streamer.ReadAsEnvelopeData(message);
            }
            catch (Exception ex)
            {
                // permanent quarantine for serialization problems
                _quarantine.Quarantine(message, ex);
                _observer.Notify(new EnvelopeDeserializationFailed(ex,"dispatch"));
                return;
            }

            if (_manager.DoWeRemember(envelope.EnvelopeId))
            {
                _observer.Notify(new EnvelopeDuplicateDiscarded(envelope.EnvelopeId));
                return;
            }
                

            try
            {
                _action(envelope);
                // non-essential but recommended
                CleanupDispatchedEnvelope(envelope);
            }
            catch (ThreadAbortException)
            {
                return;
            }
            catch (Exception ex)
            {
                if (_quarantine.TryToQuarantine(envelope, ex))
                {
                    _observer.Notify(new EnvelopeQuarantined(ex, envelope));
                    // message quarantined. Swallow
                    return;
                }
                // if we are on a persistent queue, this will tell to retry
                throw;
            }

        }

        void CleanupDispatchedEnvelope(ImmutableEnvelope envelope)
        {
            try
            {
                _manager.Memorize(envelope.EnvelopeId);
            }
            catch (ThreadAbortException)
            {
                // continue;
                throw;
            }
            catch (Exception ex)
            {
                _observer.Notify(new EnvelopeCleanupFailed(ex, envelope));
            }

            try
            {
                _quarantine.TryRelease(envelope);
            }
            catch (ThreadAbortException)
            {
                // continue
                throw;
            }
            catch (Exception ex)
            {
                _observer.Notify(new EnvelopeCleanupFailed(ex, envelope));
            }

            _observer.Notify(new EnvelopeDispatched(envelope));
        }
    }
}