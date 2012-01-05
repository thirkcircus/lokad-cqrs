#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2012 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD License
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

namespace Sample
{
    public sealed class SampleEntityVector
    {
        public long EntityId { get; private set; }

        public void Reserve(long[] indexes)
        {
            for (int i = 0; i < indexes.Length; i++)
            {
                EntityId += 1;
                indexes[i] = EntityId;
            }
        }
    }
}