#region (c) 2010-2011 Lokad CQRS - New BSD License 
// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/
#endregion

using System.Runtime.Serialization;

namespace Snippets.HttpEndpoint
{
    [DataContract]
    public class HeatPoint
    {
        [DataMember]
        public byte Intensity { get; set; }
        [DataMember]
        public int X { get; set; }
        [DataMember]
        public int Y { get; set; }

        public HeatPoint(int iX, int iY, byte bIntensity)
        {
            X = iX;
            Y = iY;
            Intensity = bIntensity;
        }
    }
}