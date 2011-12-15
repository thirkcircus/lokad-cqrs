#region (c) 2010-2011 Lokad CQRS - New BSD License 
// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/
#endregion

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Snippets.HttpEndpoint.View
{
    [DataContract]
    public class PointsView 
    {
        public PointsView()
        {
            Points = new List<HeatPoint>();
        }

        [DataMember]
        public List<HeatPoint> Points { get; set; }
    }
}