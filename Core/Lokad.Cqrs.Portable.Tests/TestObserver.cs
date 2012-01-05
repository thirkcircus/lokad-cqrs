using System;
using System.Collections.Generic;

namespace Lokad.Cqrs
{
    public static class TestObserver
    {
        sealed class Disposable : IDisposable
        {
            Action _action;
            public Disposable(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                _action();
            }
        }

        public static IDisposable When<T>(Action<T> when, bool includeTracing = true) where T : class
        {

            var eventsObserver = new ImmediateEventsObserver();

            Action<ISystemEvent> onEvent = @event =>
                {

                    var type = @event as T;

                    if (type != null)
                    {
                        when(type);
                    }
                };
            eventsObserver.Event += onEvent;
            var list = new List<IObserver<ISystemEvent>>
                {
                    eventsObserver
                };
            if (includeTracing)
            {
                list.Add(new ImmediateTracingObserver());
            }
            var old = SystemObserver.Swap(list.ToArray());

            return new Disposable(() =>
                {
                    SystemObserver.Swap(old);
                    eventsObserver.Event -= onEvent;
                });
        }
    }
}