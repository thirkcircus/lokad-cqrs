#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Generic;
using Lokad.Cqrs.Core;
using Lokad.Cqrs.Core.Outbox;

namespace Lokad.Cqrs.Build.Engine
{
    public interface IAdvancedEngineBuilder : IHideObjectMembersFromIntelliSense
    {
        void RegisterQueueWriterFactory(Func<Container, IQueueWriterFactory> activator);
        /// <summary>
        /// Registers custom module.
        /// </summary>
        /// <param name="module">The module.</param>
        void RegisterModule(IFunqlet module);
        void ConfigureContainer(Action<Container> build);

        /// <summary>
        /// Gets the list of system observers to be used by the engine.
        /// </summary>
        IList<IObserver<ISystemEvent>> Observers { get; }
        /// <summary>
        /// Allows to specify custom serializer for message envelopes (headers and transport information)
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        void CustomEnvelopeSerializer(IEnvelopeSerializer serializer);
        /// <summary>
        /// Allows to specify custom serializer for messages
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        void CustomDataSerializer(Func<Type[], IDataSerializer> serializer);
    }
}