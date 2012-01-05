using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Lokad.Cqrs.Envelope;

namespace Lokad.Cqrs
{
    public sealed class CommandHandler : HideObjectMembersFromIntelliSense
    {
        public readonly IDictionary<Type, Action<object>> Dict = new Dictionary<Type, Action<object>>();

        static readonly MethodInfo InternalPreserveStackTraceMethod =
            typeof(Exception).GetMethod("InternalPreserveStackTrace", BindingFlags.Instance | BindingFlags.NonPublic);


        public void WireToWhen(object o)
        {
            var infos = o.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.Name == "When")
                .Where(m => m.GetParameters().Length == 1);

            foreach (var methodInfo in infos)
            {
                var type = methodInfo.GetParameters().First().ParameterType;

                var info = methodInfo;
                Dict.Add(type, message => info.Invoke(o, new[] { message }));
            }
        }

        public void WireToLambda<T>(Action<T> handler)
        {
            Dict.Add(typeof(T), o => handler((T)o));
        }

        [DebuggerNonUserCode]
        public void Handle(object message)
        {
            Action<object> handler;
            var type = message.GetType();
            if (!Dict.TryGetValue(type, out handler))
            {
                //Trace.WriteLine(string.Format("Discarding {0} - failed to locate event handler", type.Name));

            }
            try
            {
                handler(message);
            }
            catch (TargetInvocationException ex)
            {
                if (null != InternalPreserveStackTraceMethod)
                    InternalPreserveStackTraceMethod.Invoke(ex.InnerException, new object[0]);
                throw ex.InnerException;
            }
        }
        public void HandleAll(ImmutableEnvelope envelope)
        {
            // we allow multiple handlers
            foreach (var message in envelope.Items)
            {
                Handle(message.Content);
            }
        }
    }

    /// <summary>
    /// Creates convention-based routing rules
    /// </summary>
    public sealed class MessageHandler : HideObjectMembersFromIntelliSense
    {
        public readonly IDictionary<Type, List<Action<object>>> Dict = new Dictionary<Type, List<Action<object>>>();

        static readonly MethodInfo InternalPreserveStackTraceMethod =
            typeof(Exception).GetMethod("InternalPreserveStackTrace", BindingFlags.Instance | BindingFlags.NonPublic);

        public void WireToWhen(object instance)
        {
            WireToObjectMethods(instance, info => info.Name == "When");
        }

        public void WireToLambda<T>(Action<T> handler)
        {
            AddHandler(typeof(T), o => handler((T)o));
        }


        public void WireToObjectMethods(object o, Predicate<MethodInfo> convention)
        {
            var infos = o.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => convention(m))
                .Where(m => m.GetParameters().Length == 1);

            foreach (var methodInfo in infos)
            {
                var type = methodInfo.GetParameters().First().ParameterType;
                var info = methodInfo;

                AddHandler(type, msg => info.Invoke(o, new[] { msg }));
            }
        }

        void AddHandler(Type type, Action<object> action)
        {
            List<Action<object>> list;
            if (!Dict.TryGetValue(type, out list))
            {
                list = new List<Action<object>>();
                Dict.Add(type, list);
            }
            list.Add(action);
        }

        public void HandleEnvelope(ImmutableEnvelope envelope)
        {
            // we allow multiple handlers
            foreach (var message in envelope.Items)
            {
                Handle(message.Content);
            }
        }

        [DebuggerNonUserCode]
        public void Handle(object @event)
        {
            List<Action<object>> info;
            var type = @event.GetType();
            if (!Dict.TryGetValue(type, out info))
            {
                //Trace.WriteLine(string.Format("Discarding {0} - failed to locate event handler", type.Name));
                return;
            }
            try
            {
                foreach (var wire in info)
                {
                    wire(@event);
                }
            }
            catch (TargetInvocationException ex)
            {
                if (null != InternalPreserveStackTraceMethod)
                    InternalPreserveStackTraceMethod.Invoke(ex.InnerException, new object[0]);
                throw ex.InnerException;
            }
        }
    }
}