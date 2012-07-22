#region (c) 2010-2012 Lokad - CQRS- New BSD License 

// Copyright (c) Lokad 2010-2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SaaS
{
    public interface ISampleMessage {}

    public interface ICommand : ISampleMessage {}

    public interface IEvent : ISampleMessage {}

    public interface ICommand<out TIdentity> : ICommand
        where TIdentity : IIdentity
    {
        TIdentity Id { get; }
    }

    public interface IApplicationService
    {
        void Execute(object command);
    }


    public interface IEvent<out TIdentity> : IEvent
        where TIdentity : IIdentity
    {
        TIdentity Id { get; }
    }

    public interface IFunctionalCommand : ICommand {}

    public interface IFunctionalEvent : IEvent {}

    /// <summary>The only messaging endpoint that is available to stateless services
    /// They are not allowed to send any other messages.</summary>
    public interface IEventPublisher
    {
        void Publish(IFunctionalEvent notification);
        void PublishBatch(IEnumerable<IFunctionalEvent> events);
    }


    public interface IAggregate<in TIdentity>
        where TIdentity : IIdentity
    {
        void Execute(ICommand<TIdentity> c);
    }


    public interface IAggregateState
    {
        void Apply(IEvent<IIdentity> e);
    }


    /// <summary>
    /// Semi strongly-typed message sending endpoint made
    ///  available to stateless workflow processes.
    /// </summary>
    public interface ICommandSender
    {
        /// <summary>
        /// This interface is intentionally made long and unusable. Generally within the domain 
        /// (as in Mousquetaires domain) there will be extension methods that provide strongly-typed
        /// extensions (that don't allow sending wrong command to wrong location).
        /// </summary>
        /// <param name="commands">The commands.</param>
        void SendCommandsAsBatch(ICommand[] commands);
    }


    public interface IEventStore
    {
        EventStream LoadEventStream(IIdentity id);
        EventStream LoadEventStream(IIdentity id, long skipEvents, int maxCount);
        /// <summary>
        /// Appends events to server stream for the provided identity.
        /// </summary>
        /// <param name="id">identity to append to.</param>
        /// <param name="expectedVersion">The expected version (specify -1 to append anyway).</param>
        /// <param name="events">The events to append.</param>
        /// <exception cref="OptimisticConcurrencyException">when new events were added to server
        /// since <paramref name="expectedVersion"/>
        /// </exception>
        void AppendToStream(IIdentity id, long expectedVersion, ICollection<IEvent> events);
    }

    public class EventStream
    {
        // version of the event stream returned
        public long Version;
        // all events in the stream
        public List<IEvent> Events = new List<IEvent>();
    }

    /// <summary>
    /// Is thrown by event store if there were changes since our last version
    /// </summary>
    [Serializable]
    public class OptimisticConcurrencyException : Exception
    {
        public long ActualVersion { get; private set; }
        public long ExpectedVersion { get; private set; }
        public IIdentity Id { get; private set; }
        public IList<IEvent> ActualEvents { get; private set; }

        OptimisticConcurrencyException(string message, long actualVersion, long expectedVersion, IIdentity id,
            IList<IEvent> serverEvents)
            : base(message)
        {
            ActualVersion = actualVersion;
            ExpectedVersion = expectedVersion;
            Id = id;
            ActualEvents = serverEvents;
        }

        public static OptimisticConcurrencyException Create(long actual, long expected, IIdentity id,
            IList<IEvent> serverEvents)
        {
            var message = string.Format("Expected v{0} but found v{1} in stream '{2}'", expected, actual, id);
            return new OptimisticConcurrencyException(message, actual, expected, id, serverEvents);
        }

        protected OptimisticConcurrencyException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }
    }

}