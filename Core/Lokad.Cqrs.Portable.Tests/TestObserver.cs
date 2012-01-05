using System;

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

        public static IDisposable When<T>(Action<T> when) where T : class
        {

            var eventsObserver = new ImmediateEventsObserver
                {
                    
                };

            Action<ISystemEvent> onEvent = @event =>
                {

                    var type = @event as T;

                    if (type != null)
                    {
                        when(type);
                    }
                };
            eventsObserver.Event += onEvent;
            var old = SystemObserver.Swap(eventsObserver, new ImmediateTracingObserver());

            return new Disposable(() =>
                {
                    SystemObserver.Swap(old);
                    eventsObserver.Event -= onEvent;
                });
        }
    }
}