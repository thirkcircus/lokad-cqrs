#region (c) 2010-2011 Lokad CQRS - New BSD License

// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;
using System.Runtime.Serialization;

namespace Snippets.HttpEndpoint
{
    [DataContract]
    public sealed class MouseMoved
    {
        [DataMember]
        public int X1 { get; set; }
        [DataMember]
        public int Y1 { get; set; }
        [DataMember]
        public int X2 { get; set; }
        [DataMember]
        public int Y2 { get; set; }

        public override string ToString()
        {
            return String.Format("x1: {0}, y1: {1}, x2: {2}, y2: {3}", X1, Y1, X2, Y2);
        }

    }

    [DataContract]
    public sealed class MouseClick
    {
        [DataMember]
        public int X { get; set; }
        [DataMember]
        public int Y { get; set; }
    }
}