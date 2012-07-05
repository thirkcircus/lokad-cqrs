#region (c) 2010-2012 Lokad - CQRS- New BSD License 

// Copyright (c) Lokad 2010-2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.IO;

namespace Lokad.Cqrs.AtomicStorage
{

    /// <summary>
    /// Logically this strategy contains two different aspects: serialization and location.
    /// However it is more convenient to keep them in one interface, since they are frequently
    /// passed together (e.g.: in projection management code)
    /// </summary>
    public interface IDocumentStrategy 
    {
        void Serialize<TEntity>(TEntity entity, Stream stream);
        TEntity Deserialize<TEntity>(Stream stream);

        string GetEntityBucket<TEntity>();
        string GetEntityLocation(Type entity, object key);
    }

    
}