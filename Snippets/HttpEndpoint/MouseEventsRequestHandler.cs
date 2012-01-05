#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;
using System.Net;
using System.Web;
using Lokad.Cqrs;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.Feature.Http;
using Lokad.Cqrs.Feature.Http.Handlers;
using Lokad.Cqrs.Partition;
using ServiceStack.Text;

namespace Snippets.HttpEndpoint
{
    public sealed class MouseEventsRequestHandler : AbstractHttpRequestHandler
    {
        readonly IQueueWriter _writer;
        readonly IDataSerializer _serializer;
        readonly IEnvelopeStreamer _streamer;

        public MouseEventsRequestHandler(IQueueWriter writer, IDataSerializer serializer, IEnvelopeStreamer streamer)
        {
            _writer = writer;
            _serializer = serializer;
            _streamer = streamer;
        }

        public override void Handle(IHttpContext context)
        {
            var contract = context.GetRequestUrl().Remove(0, "/mouseevents/".Length);

            var envelopeBuilder = new EnvelopeBuilder(contract + " - " + DateTime.Now.Ticks.ToString());

            Type contractType;
            if (!_serializer.TryGetContractTypeByName(contract, out contractType))
            {
                context.WriteString(string.Format("Trying to post command with unknown contract '{0}'.", contract));
                context.SetStatusTo(HttpStatusCode.BadRequest);
                return;
            }

            var decodedData = HttpUtility.UrlDecode(context.Request.QueryString.ToString());
            var mouseEvent = JsonSerializer.DeserializeFromString(decodedData, contractType);

            envelopeBuilder.AddItem(mouseEvent);
            _writer.PutMessage(_streamer.SaveEnvelopeData(envelopeBuilder.Build()));

            context.SetStatusTo(HttpStatusCode.OK);
        }

        public override string UrlPattern
        {
            get { return "^/mouseevents/[\\w\\.-]+$"; }
        }

        public override string[] SupportedVerbs
        {
            get { return new[] {"GET", "POST"}; }
        }
    }
}