#region (c) 2010-2011 Lokad CQRS - New BSD License 
// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/
#endregion

using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Cqrs;
using Lokad.Cqrs.AtomicStorage;
using Snippets.HttpEndpoint.View;

namespace Snippets.HttpEndpoint
{
    public class HeatMapGenerateTask : IEngineProcess
    {
        readonly IAtomicReader<unit, PointsView> _reader;
        readonly IAtomicWriter<unit, HeatMapView> _mapWriter;

        public HeatMapGenerateTask(IAtomicReader<unit, PointsView> reader, IAtomicWriter<unit, HeatMapView> mapWriter )
        {
            _reader = reader;
            _mapWriter = mapWriter;
        }

        public void Dispose()
        {}

        public void Initialize()
        {}

        public Task Start(CancellationToken token)
        {
            return Task.Factory.StartNew(() => RenderTask( token));
        }

        private void RenderTask(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var points = _reader.Get(unit.it);
                    if (points.HasValue)
                    {
                        var bitmap = new Bitmap(1280, 1024);

                        bitmap = Heatmap.CreateIntensityMask(bitmap, points.Value.Points);

                        var heatmap = Heatmap.Colorize(bitmap, 255);

                        _mapWriter.AddOrUpdate(unit.it, () => new HeatMapView(),
                            v =>
                                {
                                    v.Heatmap = heatmap;
                                    v.Thumbnail = new Bitmap(heatmap, 320, 256);
                                });
                    }
                    

                    token.WaitHandle.WaitOne(5000);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                    token.WaitHandle.WaitOne(2000);

                }
            }
        }
    }
}