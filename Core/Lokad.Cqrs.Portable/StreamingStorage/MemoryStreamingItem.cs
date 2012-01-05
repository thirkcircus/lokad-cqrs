#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Lokad.Cqrs.StreamingStorage
{
    /// <summary>
    /// Memory-based implementation of the <see cref="IStreamingItem"/>
    /// </summary>
    public sealed class MemoryStreamingItem : IStreamingItem
    {
        static readonly MD5 Md5Hash = MD5.Create();

        readonly MemoryStreamingContainer _parent;
        readonly string _path;

        byte[] _content = new byte[0];

        internal MemoryStreamingItem(MemoryStreamingContainer parent, string path)
        {
            _parent = parent;
            _path = path;
        }

        /// <summary>
        /// Performs the write operation, ensuring that the condition is met.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="condition">The condition.</param>
        /// <param name="options">The options.</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="StreamingItemIntegrityException">when integrity check fails during the upload</exception>
        public long Write(Action<Stream> writer, StreamingCondition condition = new StreamingCondition(), StreamingWriteOptions options = StreamingWriteOptions.None)
        {
            ThrowIfContainerNotFound();
            ThrowIfConditionFailed(condition);

            _parent.Add(this);

            using (var stream = new MemoryStream())
            {
                writer(stream);
                _content = stream.ToArray();
            }

            return _content.Length;
        }

        /// <summary>
        /// Attempts to read the storage item.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="condition">The condition.</param>
        /// <exception cref="StreamingItemNotFoundException">if the item does not exist.</exception>
        /// <exception cref="StreamingContainerNotFoundException">if the container for the item does not exist</exception>
        /// <exception cref="StreamingItemIntegrityException">when integrity check fails</exception>
        public void ReadInto(ReaderDelegate reader, StreamingCondition condition = new StreamingCondition())
        {
            ThrowIfContainerNotFound();
            ThrowIfItemNotFound();
            ThrowIfConditionFailed(condition);

            var props = GetUnconditionalInfo().Value;
            using (var stream = new MemoryStream(_content))
                reader(props, stream);
        }

        /// <summary>
        /// Removes the item, ensuring that the specified condition is met.
        /// </summary>
        /// <param name="condition">The condition.</param>
        public void Delete(StreamingCondition condition = new StreamingCondition())
        {
            ThrowIfContainerNotFound();
            if (!_parent.Contains(this) || !Satisfy(condition))
                return;

            _parent.Remove(this);
        }

        /// <summary>
        /// Gets the info about this item. It returns empty result if the item does not exist or does not match the condition
        /// </summary>
        /// <param name="condition">The condition.</param>
        /// <returns></returns>
        public Optional<StreamingItemInfo> GetInfo(StreamingCondition condition = new StreamingCondition())
        {
            if (_parent.Contains(this) && Satisfy(condition))
                return GetUnconditionalInfo();

            return Optional<StreamingItemInfo>.Empty;
        }

        /// <summary>
        /// Creates this storage item from another.
        /// </summary>
        /// <param name="sourceItem">The target.</param>
        /// <param name="condition">The condition.</param>
        /// <param name="copySourceCondition">The copy source condition.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="StreamingItemNotFoundException">when source storage is not found</exception>
        /// <exception cref="StreamingItemIntegrityException">when integrity check fails</exception>
        public void CopyFrom(IStreamingItem sourceItem, StreamingCondition condition = new StreamingCondition(), StreamingCondition copySourceCondition = new StreamingCondition(), StreamingWriteOptions options = StreamingWriteOptions.None)
        {
            var source = sourceItem as MemoryStreamingItem;
            if (source != null)
            {
                ThrowIfContainerNotFound();
                ThrowIfConditionFailed(condition);

                source.ThrowIfContainerNotFound();
                source.ThrowIfItemNotFound();
                source.ThrowIfConditionFailed(copySourceCondition);

                _content = source._content;
                _parent.Add(this);
            }
            else
                Write(targetStream => sourceItem.ReadInto((props, stream) => stream.CopyTo(targetStream, 65536), copySourceCondition), condition, options);
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
                throw StreamingErrors.ContainerNotFound(this);
        }

        void ThrowIfItemNotFound()
        {
            if (!_parent.Contains(this))
                throw StreamingErrors.ItemNotFound(this);
        }

        void ThrowIfConditionFailed(StreamingCondition condition)
        {
            if (!Satisfy(condition))
                throw StreamingErrors.ConditionFailed(this, condition);
        }

        bool Satisfy(StreamingCondition condition)
        {
            return
                GetUnconditionalInfo().Convert(
                    s => new LocalStreamingInfo(s.ETag)).Convert(
                        s => condition.Satisfy(new[] { s }),
                        () => condition.Satisfy(new LocalStreamingInfo[0]));
        }

        Optional<StreamingItemInfo> GetUnconditionalInfo()
        {
            if (!_parent.ListItems().Contains(Path.GetFileName(_path)))
                return Optional<StreamingItemInfo>.Empty;

            var eTag = BitConverter.ToString(Md5Hash.ComputeHash(_content)).Replace("-", "");
            return new StreamingItemInfo(eTag, new NameValueCollection(0), new Dictionary<string, string>(0));
        }
    }
}
