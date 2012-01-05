#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2012 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD License
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;

namespace Audit.Views
{
    public sealed class ViewMapInfo
    {
        public readonly Type[] Events;
        public readonly Type Projection;
        public readonly Type ViewType;
        public readonly Type KeyType;


        public ViewMapInfo(Type viewType, Type keyType, Type projection, Type[] events)
        {
            ViewType = viewType;
            Projection = projection;
            Events = events;
            KeyType = keyType;
        }
    }
}