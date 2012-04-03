#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.IO;
using System.Linq;

namespace Lokad.Cqrs.StreamingStorage
{
    /// <summary>
    /// Memory-based implementation of the <see cref="IStreamItem"/>
    /// </summary>
    public sealed class MemoryStreamItem : IStreamItem
    {
        readonly MemoryStreamContainer _parent;
        readonly string _path;

        byte[] _content = new byte[0];

        internal MemoryStreamItem(MemoryStreamContainer parent, string path)
        {
            _parent = parent;
            _path = path;
        }

        /// <summary>
        /// Performs the write operation, ensuring that the condition is met.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="StreamingItemIntegrityException">when integrity check fails during the upload</exception>
        public long Write(Action<Stream> writer)
        {
            ThrowIfContainerNotFound();

            _parent.Add(this);

            using (var stream = new MemoryStream())
            {
                writer(stream);
                _content = stream.ToArray();
            }

            return _content.Length;
        }

        public bool Exists()
        {
            return _parent.Contains(this);
        }

        /// <summary>
        /// Attempts to read the storage item.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="condition">The condition.</param>
        /// <exception cref="StreamItemNotFoundException">if the item does not exist.</exception>
        /// <exception cref="StreamContainerNotFoundException">if the container for the item does not exist</exception>
        /// <exception cref="StreamingItemIntegrityException">when integrity check fails</exception>
        public void ReadInto(Action<Stream> reader)
        {
            ThrowIfContainerNotFound();
            ThrowIfItemNotFound();
            
            using (var stream = new MemoryStream(_content))
                reader(stream);
        }

        /// <summary>
        /// Removes the item, ensuring that the specified condition is met.
        /// </summary>
        public void Delete()
        {
            ThrowIfContainerNotFound();
            if (!_parent.Contains(this))
                return;

            _parent.Remove(this);
        }

        /// <summary>
        /// Gets the full path of the current item.
        /// </summary>
        /// <value>The full path.</value>
        public string FullPath
        {
            get { return _path; }
        }

        void ThrowIfContainerNotFound()
        {
            if (!_parent.Exists())
                throw StreamErrors.ContainerNotFound(this);
        }

        void ThrowIfItemNotFound()
        {
            if (!_parent.Contains(this))
                throw StreamErrors.ItemNotFound(this);
        }
    }
}
