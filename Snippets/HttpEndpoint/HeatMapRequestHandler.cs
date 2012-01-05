#region (c) 2010-2011 Lokad CQRS - New BSD License 
// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/
#endregion

using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using Lokad.Cqrs;
using Lokad.Cqrs.AtomicStorage;
using Lokad.Cqrs.Feature.Http;
using Lokad.Cqrs.Feature.Http.Handlers;
using Snippets.HttpEndpoint.View;

namespace Snippets.HttpEndpoint
{
    public class HeatMapRequestHandler : AbstractHttpRequestHandler 
    {
        readonly IAtomicReader<unit, HeatMapView> _reader;

        public HeatMapRequestHandler(IAtomicReader<unit, HeatMapView> reader )
        {
            _reader = reader;
        }

        public override string UrlPattern
        {
            get { return "/heatmap"; }
        }

        public override string[] SupportedVerbs
        {
            get { return new[] {"GET"}; }
        }

        public override void Handle(IHttpContext context)
        {
            var view = _reader.Get(unit.it);

            if (view.HasValue)
            {
                if (context.GetRequestUrl().Contains("thumb"))
                {
                    view.Value.Thumbnail.Save(context.Response.OutputStream, ImageFormat.Jpeg);
                }
                else
                {
                    view.Value.Heatmap.Save(context.Response.OutputStream, ImageFormat.Jpeg);
                }
            }
            else
            {
                ReturnEmptyImage(context);
            }

            context.SetStatusTo(HttpStatusCode.OK);
        }

        static void ReturnEmptyImage(IHttpContext context)
        {
            var bitmap = new Bitmap(1, 1);
            bitmap.Save(context.Response.OutputStream, ImageFormat.Jpeg);
        }
    }
}