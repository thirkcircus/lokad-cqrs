#region (c) 2010-2011 Lokad CQRS - New BSD License 
// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/
#endregion

using System.Drawing;
using System.Runtime.Serialization;

namespace Snippets.HttpEndpoint.View
{
    [DataContract]
    public class HeatMapView 
    {
        public HeatMapView()
        {
            Heatmap = new Bitmap(1,1);
            Thumbnail = new Bitmap(1, 1);
        }

        [DataMember]
        public Bitmap Heatmap { get; set; }
        
        [DataMember]
        public Bitmap Thumbnail { get; set; }
    }
}