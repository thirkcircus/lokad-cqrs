#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

namespace Lokad.Cqrs.StreamingStorage
{
    /// <summary>
    /// Storage root using memory for persisting data
    /// </summary>
    public sealed class MemoryStreamingRoot : IStreamingRoot
    {
        readonly MemoryStreamingContainer _root = new MemoryStreamingContainer(null, "");

        public IStreamingContainer GetContainer(string name)
        {
            return _root.GetContainer(name);
        }
    }
}
