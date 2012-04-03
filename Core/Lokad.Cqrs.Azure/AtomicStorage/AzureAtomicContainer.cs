#region (c) 2010-2012 Lokad - CQRS Sample for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.StorageClient;

namespace Lokad.Cqrs.AtomicStorage
{
    public sealed class AzureAtomicContainer : IDocumentStore
    {
        public IDocumentWriter<TKey, TEntity> GetWriter<TKey, TEntity>()
        {
            var writer = new AzureAtomicWriter<TKey, TEntity>(_directory, _strategy);

            var value = Tuple.Create(typeof(TKey), typeof(TEntity));
            if (_initialized.Add(value))
            {
                // we've added a new record. Need to initialize
                writer.InitializeIfNeeded();
            }
            return writer;
        }

        public override string ToString()
        {
            return _directory.Uri.AbsoluteUri;
        }

        public IDocumentReader<TKey, TEntity> GetReader<TKey, TEntity>()
        {
            return new AzureAtomicReader<TKey, TEntity>(_directory, _strategy);
        }

        public IDocumentStrategy Strategy
        {
            get { return _strategy; }
        }

        

        public void Reset(string bucket)
        {
            var blobs =  _directory.GetSubdirectory(bucket).ListBlobs(new BlobRequestOptions { UseFlatBlobListing = true });
            var c = _directory.ServiceClient;
            foreach (var listBlobItem in blobs.AsParallel())
            {
                c.GetBlobReference(listBlobItem.Uri.ToString()).DeleteIfExists();
            }
        }

        public void ResetAll()
        {
            var blobs = _directory.ListBlobs(new BlobRequestOptions { UseFlatBlobListing = true });
            var c = _directory.ServiceClient;
            foreach (var listBlobItem in blobs.AsParallel())
            {
                c.GetBlobReference(listBlobItem.Uri.ToString()).DeleteIfExists();
            }
        }

        public IEnumerable<DocumentRecord> EnumerateContents(string bucket)
        {
            var subdir = _directory.GetSubdirectory(bucket);
            var l = subdir.ListBlobs(new BlobRequestOptions {UseFlatBlobListing = true});
            foreach (var item in l)
            {
                var blob = subdir.GetBlobReference(item.Uri.ToString());
                var rel = subdir.Uri.MakeRelativeUri(item.Uri).ToString();
                yield return new DocumentRecord(rel.Replace('\\', '/'), blob.DownloadByteArray);
            }
        }

        public void WriteContents(string bucket, IEnumerable<DocumentRecord> records)
        {
            var cloudBlobDirectory = _directory.GetSubdirectory(bucket);
            foreach (var atomicRecord in records)
            {
                cloudBlobDirectory.GetBlobReference(atomicRecord.Key).UploadByteArray(atomicRecord.Read());
            }
        }

        


        readonly IDocumentStrategy _strategy;

        readonly HashSet<Tuple<Type, Type>> _initialized = new HashSet<Tuple<Type, Type>>();
        readonly CloudBlobDirectory _directory;

        public AzureAtomicContainer(IDocumentStrategy strategy, CloudBlobDirectory directory)
        {
            _strategy = strategy;
            _directory = directory;
        }
    }
}