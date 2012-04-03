#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using Lokad.Cqrs.StreamingStorage;
using Microsoft.WindowsAzure.StorageClient;

namespace Lokad.Cqrs.Feature.StreamingStorage
{
    /// <summary>
    /// Azure BLOB implementation of the <see cref="IStreamItem"/>
    /// </summary>
    public sealed class BlobStreamingItem : IStreamItem
    {
        readonly CloudBlob _blob;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobStreamingItem"/> class.
        /// </summary>
        /// <param name="blob">The BLOB.</param>
        public BlobStreamingItem(CloudBlob blob)
        {
            _blob = blob;
        }

        //const string ContentCompression = "gzip";

        /// <summary>
        /// Performs the write operation, ensuring that the condition is met.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="condition">The condition.</param>
        /// <param name="writeOptions">The write options.</param>
        public long Write(Action<Stream> writer)
        {
            try
            {
                long position;
                using (var stream = _blob.OpenWrite())
                {
                    using (var compress = new GZipStream(stream, CompressionMode.Compress, true))
                    {
                        writer(compress);
                    }
                    position = stream.Position;
                }
                return position;
            }
            catch (StorageClientException ex)
            {
                switch (ex.ErrorCode)
                {

                    case StorageErrorCode.ContainerNotFound:
                        throw StreamErrors.ContainerNotFound(this, ex);
                    default:
                        throw;
                }
            }
        }

        public bool Exists()
        {
            try
            {
                _blob.FetchAttributes();
                return true;
            }
            catch(StorageClientException ex)
            {
                return false;
            }
            
        }

        /// <summary>
        /// Attempts to read the storage item.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="condition">The condition.</param>
        /// <exception cref="StreamingItemNotFoundException">if the item does not exist.</exception>
        /// <exception cref="StreamContainerNotFoundException">if the container for the item does not exist</exception>
        /// <exception cref="StreamingItemIntegrityException">when integrity check fails</exception>
        public void ReadInto(Action<Stream> reader)
        {
            try
            {
                using (var stream = _blob.OpenRead())
                {
                    using (var decompress = new GZipStream(stream, CompressionMode.Decompress, true))
                    {
                        reader(decompress);
                    }

                }
            }
            
            catch (StorageClientException e)
            {
                switch (e.ErrorCode)
                {
                    case StorageErrorCode.ContainerNotFound:
                        throw StreamErrors.ContainerNotFound(this, e);
                    case StorageErrorCode.ResourceNotFound:
                    case StorageErrorCode.BlobNotFound:
                        throw StreamErrors.ItemNotFound(this, e);

                    default:
                        throw;
                }
            }
        }

        /// <summary>
        /// Removes the item, ensuring that the specified condition is met.
        /// </summary>
        /// <param name="condition">The condition.</param>
        public void Delete()
        {
            try
            {
                _blob.Delete();
            }
            catch (StorageClientException ex)
            {
                switch (ex.ErrorCode)
                {
                    case StorageErrorCode.ContainerNotFound:
                        throw StreamErrors.ContainerNotFound(this, ex);
                    case StorageErrorCode.BlobNotFound:
                    case StorageErrorCode.ConditionFailed:
                        return;
                    default:
                        throw;
                }
            }
        }



        /// <summary>
        /// Gets the full path of the current item.
        /// </summary>
        /// <value>The full path.</value>
        public string FullPath
        {
            get { return _blob.Uri.ToString(); }
        }

        /// <summary>
        /// Gets the BLOB reference behind this instance.
        /// </summary>
        /// <value>The reference.</value>
        public CloudBlob Reference
        {
            get { return _blob; }
        }

 
        static T ExposeException<T>(Optional<T> optional, string message)
        {
            if (message == null) throw new ArgumentNullException(@"message");
            if (!optional.HasValue)
            {

                throw new InvalidOperationException(message);
            }
            return optional.Value;
        }
    }
}