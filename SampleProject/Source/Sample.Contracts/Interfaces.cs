#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2012 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD License
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;

namespace Sample
{
    public interface ISampleMessage {}

    public interface ISampleCommand : ISampleMessage {}

    public interface ISampleEvent : ISampleMessage {}

    public interface ICommand<out TIdentity> : ISampleCommand
        where TIdentity : IIdentity
    {
        TIdentity Id { get; }
    }

    public interface IEvent<out TIdentity> : ISampleEvent
        where TIdentity : IIdentity
    {
        TIdentity Id { get; }
    }

    public interface IFunctionalCommand : ISampleCommand {}

    public interface IFunctionalEvent : ISampleEvent {}

    /// <summary>The only messaging endpoint that is available to stateless services
    /// They are not allowed to send any other messages.</summary>
    public interface IServiceEndpoint
    {
        void Publish(IFunctionalEvent notification);
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
    public interface IFunctionalFlow
    {
        void Schedule(ISampleCommand command, DateTime dateUtc);

        /// <summary>
        /// This interface is intentionally made long and unusable. Generally within the domain 
        /// (as in Mousquetaires domain) there will be extension methods that provide strongly-typed
        /// extensions (that don't allow sending wrong command to wrong location).
        /// </summary>
        /// <param name="commands">The commands.</param>
        void SendCommandsAsBatch(ISampleCommand[] commands);
    }
}