#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.IO;
using System.Threading;
using Lokad.Cqrs.Core.Outbox;

namespace Lokad.Cqrs.Feature.FilePartition
{
    public sealed class FileQueueWriter : IQueueWriter
    {
        readonly DirectoryInfo _folder;
        readonly IEnvelopeStreamer _streamer;

        public string Name { get; private set; }
        public readonly string Suffix ;

        public FileQueueWriter(DirectoryInfo folder, string name, IEnvelopeStreamer streamer)
        {
            _folder = folder;
            _streamer = streamer;
            Name = name;
            Suffix = Guid.NewGuid().ToString().Substring(0, 4);
        }

            static long UniversalCounter;

            public void PutMessage(ImmutableEnvelope envelope)
            {
                var id = Interlocked.Increment(ref UniversalCounter);
                var fileName = string.Format("{0:yyyy-MM-dd-HH-mm-ss}-{1:00000000}-{2}", envelope.CreatedOnUtc, id, Suffix);
                var full = Path.Combine(_folder.FullName, fileName);
                var data = _streamer.SaveEnvelopeData(envelope);
                File.WriteAllBytes(full, data);
            }
    }
}