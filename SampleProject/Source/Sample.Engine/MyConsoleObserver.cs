using System;
using System.Diagnostics;
using Lokad.Cqrs;
using Lokad.Cqrs.Envelope.Events;

namespace Sample.Engine
{
    sealed class MyConsoleObserver : IObserver<ISystemEvent>
    {
        readonly Stopwatch _watch = Stopwatch.StartNew();

        public void OnNext(ISystemEvent value)
        {
            RedirectToWhen.InvokeEventOptional(this, value);
        }

        void When(EnvelopeDispatched ed)
        {
            if (ed.Dispatcher == "router")
            {
                foreach (var item in ed.Envelope.Items)
                {
                    var prefix = "";
                    if (item.Content is ICommand<IIdentity>)
                    {
                        prefix = ((ICommand<IIdentity>) (item.Content)).Id + " ";
                    }
                    else if (item.Content is IEvent<IIdentity>)
                    {
                        prefix = ((IEvent<IIdentity>) (item.Content)).Id + " ";
                    }
                    WriteLine(prefix + Describe.Object(item.Content));
                }
            }
        }

        void When(EnvelopeQuarantined e)
        {
            WriteLine(e.LastException.ToString());
        }

        void WriteLine(string line)
        {
            Console.WriteLine("[{0:0000000}]: {1}", _watch.ElapsedMilliseconds, line);
        }


        public void OnError(Exception error) {}

        public void OnCompleted() {}
    }
}