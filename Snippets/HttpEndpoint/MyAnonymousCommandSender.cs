#region (c) 2010-2011 Lokad CQRS - New BSD License

// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;
using System.Net;
using Lokad.Cqrs;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.Feature.Http;
using Lokad.Cqrs.Feature.Http.Handlers;
using Lokad.Cqrs.Partition;

namespace Snippets.HttpEndpoint
{
    public sealed class MyAnonymousCommandSender : AbstractHttpRequestHandler
    {
        readonly IQueueWriter _writer;
        readonly IDataSerializer _serializer;
        readonly IEnvelopeStreamer _streamer;
        public MyAnonymousCommandSender(IQueueWriter writer, IDataSerializer serializer, IEnvelopeStreamer streamer)
        {
            _writer = writer;
            _serializer = serializer;
            _streamer = streamer;
        }

        public override string UrlPattern
        {
            get { return "^/send/[\\w\\.-]+$"; }
        }

        public override string[] SupportedVerbs
        {
            get { return new[] { "POST", "GET"}; }
        }

        public override void Handle(IHttpContext context)
        {
            var msg = new EnvelopeBuilder(Guid.NewGuid().ToString());

            var contract = context.GetRequestUrl().Remove(0,"/send/".Length);
            Type contractType;
            if (!_serializer.TryGetContractTypeByName(contract, out contractType))
            {
                context.WriteString(string.Format("Trying to post command with unknown contract '{0}'.", contract));
                context.SetStatusTo(HttpStatusCode.BadRequest);
                return;
            }

            _writer.PutMessage(_streamer.SaveEnvelopeData(msg.Build()));
            context.WriteString(string.Format(@"
Normally this should be a JSON POST, containing serialized data for {0}
but let's pretend that you successfully sent a message. Or routed it", contractType));


            context.SetStatusTo(HttpStatusCode.OK);
        }
    }
}