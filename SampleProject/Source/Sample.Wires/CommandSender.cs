#region (c) 2010-2012 Lokad - CQRS Sample for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using Lokad.Cqrs;

namespace Sample.Wires
{
    public sealed class CommandSender : ICommandSender
    {
        readonly SimpleMessageSender _sender;

        public CommandSender(SimpleMessageSender sender)
        {
            _sender = sender;
        }


        public void SendCommandsAsBatch(ISampleCommand[] commands)
        {
            _sender.SendBatch(commands);
        }
    }
}