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
    public sealed class AzureAtomicContainer : IAtomicContainer
    {
        public IAtomicWriter<TKey, TEntity> GetEntityWriter<TKey, TEntity>()
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

        public IAtomicReader<TKey, TEntity> GetEntityReader<TKey, TEntity>()
        {
            return new AzureAtomicReader<TKey, TEntity>(_directory, _strategy);
        }

        public IAtomicStorageStrategy Strategy
        {
            get { return _strategy; }
        }

        public IEnumerable<AtomicRecord> EnumerateContents()
        {
            var l = _directory.ListBlobs(new BlobRequestOptions {UseFlatBlobListing = true});
            foreach (var item in l)
            {
                var blob = _directory.GetBlobReference(item.Uri.ToString());
                var rel = _directory.Uri.MakeRelativeUri(item.Uri).ToString();
                yield return new AtomicRecord(rel.Replace('\\', '/'), blob.DownloadByteArray);
            }
        }

        public void WriteContents(IEnumerable<AtomicRecord> records)
        {
            foreach (var atomicRecord in records)
            {
                _directory.GetBlobReference(atomicRecord.Path).UploadByteArray(atomicRecord.Read());
            }
        }

        public void Reset()
        {
            var blobs = _directory.ListBlobs(new BlobRequestOptions {UseFlatBlobListing = true});
            var c = _directory.ServiceClient;
            foreach (var listBlobItem in blobs.AsParallel())
            {
                c.GetBlobReference(listBlobItem.Uri.ToString()).DeleteIfExists();
            }
        }


        readonly IAtomicStorageStrategy _strategy;

        readonly HashSet<Tuple<Type, Type>> _initialized = new HashSet<Tuple<Type, Type>>();
        readonly CloudBlobDirectory _directory;

        public AzureAtomicContainer(IAtomicStorageStrategy strategy, CloudBlobDirectory directory)
        {
            _strategy = strategy;
            _directory = directory;
        }
    }
}