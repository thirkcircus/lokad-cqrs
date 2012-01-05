#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Lokad.Cqrs.StreamingStorage
{
    /// <summary>
    /// Storage container using memory for persisting data
    /// </summary>
    public sealed class MemoryStreamingContainer : IStreamingContainer
    {
        readonly string _path;
        readonly MemoryStreamingContainer _parent;

        readonly ConcurrentDictionary<string, MemoryStreamingContainer> _containers = new ConcurrentDictionary<string, MemoryStreamingContainer>();
        readonly ConcurrentDictionary<string, IStreamingItem> _items = new ConcurrentDictionary<string, IStreamingItem>();

        internal MemoryStreamingContainer(MemoryStreamingContainer parent, string path)
        {
            _parent = parent;
            _path = path;
        }

        public IStreamingContainer GetContainer(string name)
        {
            var path = Path.Combine(_path, name);

            MemoryStreamingContainer container;
            if (!GetRealContainer()._containers.TryGetValue(path, out container))
            {
                container = new MemoryStreamingContainer(this, path);
            }

            return container;
        }

        public IStreamingItem GetItem(string name)
        {
            var path = Path.Combine(_path, name);

            IStreamingItem item;
            if (!GetRealContainer()._items.TryGetValue(path, out item))
            {
                item = new MemoryStreamingItem(this, path);
            }

            return item;
        }

        MemoryStreamingContainer GetRealContainer()
        {
            if (_parent == null)
                return this;

            return _parent.GetExistingContainerOr(this);
        }

        MemoryStreamingContainer GetExistingContainerOr(MemoryStreamingContainer container)
        {
            MemoryStreamingContainer cntr;
            return _containers.TryGetValue(container.FullPath, out cntr) ? cntr : container;
        }

        public IStreamingContainer Create()
        {
            if (_parent == null)
                return this;

            return GetRealContainer()._parent.Add(this);
        }

        public void Delete()
        {
            var realContainer = GetRealContainer();

            foreach (var container in realContainer._containers.Values.ToArray())
                container.Delete();

            foreach (var item in realContainer._items.Values.ToArray())
                item.Delete();

            if (_parent != null)
                realContainer._parent.Remove(this);
        }

        public bool Exists()
        {
            return _parent == null || GetRealContainer()._parent.Contains(this);
        }

        public IEnumerable<string> ListItems()
        {
            ThrowIfContainerNotFound();
            return GetRealContainer()._items.Keys.Select(Path.GetFileName);
        }

        public string FullPath
        {
            get { return _path; }
        }

        internal MemoryStreamingContainer Add(MemoryStreamingContainer container)
        {
            ThrowIfContainerNotFound();

            return GetRealContainer()._containers.GetOrAdd(container.FullPath, s => container);
        }

        internal void Remove(IStreamingContainer container)
        {
            MemoryStreamingContainer dummy;
            GetRealContainer()._containers.TryRemove(container.FullPath, out dummy);
        }

        internal bool Contains(IStreamingContainer container)
        {
            return GetRealContainer()._containers.ContainsKey(container.FullPath);
        }

        internal void Add(MemoryStreamingItem item)
        {
            GetRealContainer()._items.TryAdd(item.FullPath, item);
        }

        internal void Remove(MemoryStreamingItem item)
        {
            IStreamingItem dummy;
            GetRealContainer()._items.TryRemove(item.FullPath, out dummy);
        }

        internal bool Contains(IStreamingItem item)
        {
            return GetRealContainer()._items.ContainsKey(item.FullPath);
        }

        void ThrowIfContainerNotFound()
        {
            if (!Exists())
                throw StreamingErrors.ContainerNotFound(this);
        }
    }
}
