#region Copyright (c) 2006-2011 LOKAD SAS. All rights reserved

// You must not remove this notice, or any other, from this software.
// This document is the property of LOKAD SAS and must not be disclosed

#endregion

using System;
using Lokad.Cqrs;
using Lokad.Cqrs.Envelope;

namespace Sample.Wires
{
    public sealed class MessageSender : IFunctionalFlow
    {
        readonly SimpleMessageSender _sender;

        public MessageSender(SimpleMessageSender sender)
        {
            _sender = sender;
        }

        public void Schedule(ISampleCommand command, DateTime dateUtc)
        {
            _sender.SendOne(command, eb => eb.DeliverOnUtc(dateUtc));
        }

        public void SendCommandsAsBatch(ISampleCommand[] commands)
        {
            _sender.SendBatch(commands);
        }
    }
}