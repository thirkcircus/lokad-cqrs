#region (c) 2010-2011 Lokad CQRS - New BSD License 
// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/
#endregion

using System.Drawing;
using System.Runtime.Serialization;
using Lokad.Cqrs;

namespace Snippets.HttpEndpoint.View
{
    [DataContract]
    public class HeatMapView : Define.AtomicSingleton
    {
        [DataMember]
        public Bitmap Heatmap { get; set; }
    }
}