#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Diagnostics;

namespace Lokad.Cqrs
{
    public static class SystemObserver 
    {
        static IObserver<ISystemEvent>[] _observers = new IObserver<ISystemEvent>[0];

        
   
        public static IObserver<ISystemEvent>[] Swap(params IObserver<ISystemEvent>[] swap)
        {
            var old = _observers;
            _observers = swap;
            return old;
        }

        public static void  Notify(ISystemEvent @event)
        {
            foreach (var observer in _observers)
            {
                try
                {
                    observer.OnNext(@event);
                }
                catch (Exception ex)
                {
                    var message = string.Format("Observer {0} failed with {1}", observer, ex);
                    Trace.WriteLine(message);
                }
            }
        }



        public static void Complete()
        {
            foreach (var observer in _observers)
            {
                observer.OnCompleted();
            }
        }
    }
}