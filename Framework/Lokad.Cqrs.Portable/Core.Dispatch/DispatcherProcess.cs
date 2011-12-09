#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Cqrs.Core.Dispatch.Events;
using Lokad.Cqrs.Core.Inbox;

namespace Lokad.Cqrs.Core.Dispatch
{
    /// <summary>
    /// Engine process that coordinates pulling messages from queues and
    /// dispatching them to the specified handlers
    /// </summary>
    public sealed class DispatcherProcess : IEngineProcess
    {
        readonly Action<byte[]> _dispatcher;
        readonly ISystemObserver _observer;
        readonly IPartitionInbox _inbox;

        public DispatcherProcess(
            ISystemObserver observer,
            Action<byte[]> dispatcher, 
            IPartitionInbox inbox)
        {
            _dispatcher = dispatcher;
            _observer = observer;
            _inbox = inbox;
        }

        public void Dispose()
        {
            _disposal.Dispose();
        }

        public void Initialize()
        {
            _inbox.Init();
        }

        readonly CancellationTokenSource _disposal = new CancellationTokenSource();

        public Task Start(CancellationToken token)
        {
            return Task.Factory
                .StartNew(() =>
                    {
                        try
                        {
                            ReceiveMessages(token);
                        }
                        catch(ObjectDisposedException)
                        {
                            // suppress
                        }
                    }, token);
        }

        void ReceiveMessages(CancellationToken outer)
        {
            using (var source = CancellationTokenSource.CreateLinkedTokenSource(_disposal.Token, outer))
            {
                while (true)
                {
                    MessageTransportContext context;
                    try
                    {
                        if (!_inbox.TakeMessage(source.Token, out context))
                        {
                            // we didn't retrieve message within the token lifetime.
                            // it's time to shutdown the server
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        // unexpected but possible: retry
                        _observer.Notify(new MessageInboxFailed(ex, _inbox.ToString(),"@dispatch"));
                        continue;
                    }
                    
                    try
                    {
                        ProcessMessage(context);
                    }
                    catch (ThreadAbortException)
                    {
                         // Nothing. we are being shutdown
                    }
                    catch(Exception ex)
                    {
                        var e = new DispatchRecoveryFailed(ex, context, context.QueueName);
                        _observer.Notify(e);
                    }
                }
            }
        }

        void ProcessMessage(MessageTransportContext context)
        {
            var dispatched = false;
            try
            {
                _dispatcher(context.Unpacked);

                dispatched = true;
            }
            catch (ThreadAbortException)
            {
                // we are shutting down. Stop immediately
                return;
            }
            catch (Exception dispatchEx)
            {
                // if the code below fails, it will just cause everything to be reprocessed later,
                // which is OK (duplication manager will handle this)

                _observer.Notify(new MessageDispatchFailed(context, context.QueueName, dispatchEx));
                // quarantine is atomic with the processing
                _inbox.TryNotifyNack(context);
            }
            if (!dispatched)
                return;
            try
            {
                _inbox.AckMessage(context);
                // 3rd - notify.
                _observer.Notify(new MessageAcked(context));

            }
            catch (ThreadAbortException)
            {
                // nothing. We are going to sleep
            }
            catch (Exception ex)
            {
                // not a big deal. Message will be processed again.
                _observer.Notify(new MessageAckFailed(ex, context));
            }
        }
    }
}