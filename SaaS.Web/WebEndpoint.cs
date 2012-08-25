#region (c) 2010-2012 Lokad - CQRS- New BSD License 

// Copyright (c) Lokad 2010-2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Globalization;
using Lokad.Cqrs;
using Lokad.Cqrs.AtomicStorage;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.Partition;
using Lokad.Cqrs.StreamingStorage;
using SaaS.Wires;

namespace SaaS.Web
{
    public sealed class Client
    {
        readonly NuclearStorage _store;
        readonly TypedMessageSender _sender;
        readonly IStreamRoot _root;

        public Client(IDocumentStore store, TypedMessageSender sender, IStreamRoot root)
        {
            _store = new NuclearStorage(store);
            _sender = sender;
            _root = root;
        }


        public void SendCommand(ICommand cmd, string optionalId = null)
        {
            var envelopeId = optionalId ?? Guid.NewGuid().ToString().ToLowerInvariant();
            _sender.SendFromClient(cmd, envelopeId, GetSessionInfo());
        }
        static MessageAttribute[] GetSessionInfo()
        {
            var auth = FormsAuth.GetSessionIdentityFromRequest();
            if (auth.HasValue)
            {
                return new[]
                    {
                        new MessageAttribute("web-user",auth.Value.User.Id.ToString(CultureInfo.InvariantCulture)), 
                        new MessageAttribute("web-token",auth.Value.Token), 
                    };
            }
            return new MessageAttribute[0];
        }

        public TSingleton GetSingleton<TSingleton>() where TSingleton : new()
        {
            return _store.GetSingletonOrNew<TSingleton>();
        }

        public Maybe<TEntity> GetView<TEntity>(object key)
        {
            return _store.GetEntity<TEntity>(key);
        }

        public TEntity GetViewOrThrow<TEntity>(object key)
        {
            var optional = GetView<TEntity>(key);
            if (optional.HasValue)
                return optional.Value;
            throw new InvalidOperationException(string.Format("Failed to locate view {0} by key {1}", typeof(TEntity),
                key));
        }
        //public void UnzipTo(BinaryReference reference, Stream outputStream)
        //{
        //    using (var output = _root.GetContainer(reference.Container).OpenRead(reference.Name))
        //    {
        //        if (reference.Compressed)
        //        {
        //            using (var decompress = new GZipStream(output, CompressionMode.Decompress))
        //            {
        //                decompress.CopyTo(outputStream);
        //            }
        //        }
        //        else
        //        {
        //            output.CopyTo(outputStream);
        //        }
        //    }
        //}
    }
}