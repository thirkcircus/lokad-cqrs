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
    public sealed class MemoryStreamContainer : IStreamContainer
    {
        readonly string _path;
        readonly MemoryStreamContainer _parent;

        readonly ConcurrentDictionary<string, MemoryStreamContainer> _containers = new ConcurrentDictionary<string, MemoryStreamContainer>();
        readonly ConcurrentDictionary<string, IStreamItem> _items = new ConcurrentDictionary<string, IStreamItem>();

        public MemoryStreamContainer(MemoryStreamContainer parent, string path)
        {
            _parent = parent;
            _path = path;
        }

        public IStreamContainer GetContainer(string name)
        {
            var path = Path.Combine(_path, name);

            MemoryStreamContainer container;
            if (!GetRealContainer()._containers.TryGetValue(path, out container))
            {
                container = new MemoryStreamContainer(this, path);
            }

            return container;
        }

        public IStreamItem GetItem(string name)
        {
            var path = Path.Combine(_path, name);

            IStreamItem item;
            if (!GetRealContainer()._items.TryGetValue(path, out item))
            {
                item = new MemoryStreamItem(this, path);
            }

            return item;
        }

        MemoryStreamContainer GetRealContainer()
        {
            if (_parent == null)
                return this;

            return _parent.GetExistingContainerOr(this);
        }

        MemoryStreamContainer GetExistingContainerOr(MemoryStreamContainer container)
        {
            MemoryStreamContainer cntr;
            return _containers.TryGetValue(container.FullPath, out cntr) ? cntr : container;
        }

        public IStreamContainer Create()
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

        internal MemoryStreamContainer Add(MemoryStreamContainer container)
        {
            ThrowIfContainerNotFound();

            return GetRealContainer()._containers.GetOrAdd(container.FullPath, s => container);
        }

        internal void Remove(IStreamContainer container)
        {
            MemoryStreamContainer dummy;
            GetRealContainer()._containers.TryRemove(container.FullPath, out dummy);
        }

        internal bool Contains(IStreamContainer container)
        {
            return GetRealContainer()._containers.ContainsKey(container.FullPath);
        }

        internal void Add(MemoryStreamItem item)
        {
            GetRealContainer()._items.TryAdd(item.FullPath, item);
        }

        internal void Remove(MemoryStreamItem item)
        {
            IStreamItem dummy;
            GetRealContainer()._items.TryRemove(item.FullPath, out dummy);
        }

        internal bool Contains(IStreamItem item)
        {
            return GetRealContainer()._items.ContainsKey(item.FullPath);
        }

        void ThrowIfContainerNotFound()
        {
            if (!Exists())
                throw StreamErrors.ContainerNotFound(this);
        }
    }
}
