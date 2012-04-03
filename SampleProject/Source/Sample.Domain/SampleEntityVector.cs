#region (c) 2010-2012 Lokad - CQRS Sample for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

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