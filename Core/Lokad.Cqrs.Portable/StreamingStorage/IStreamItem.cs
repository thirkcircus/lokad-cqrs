#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.IO;

namespace Lokad.Cqrs.StreamingStorage
{
    /// <summary>
    /// Base interface for performing storage operations against local or remote persistence.
    /// </summary>
    public interface IStreamItem
    {
        /// <summary>
        /// Gets the full path of the current iteб.
        /// </summary>
        /// <value>The full path.</value>
        string FullPath { get; }

        /// <summary>
        /// Performs the write operation, ensuring that the condition is met.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <returns>number of bytes written</returns>
        long Write(Action<Stream> writer);

        bool Exists();

        /// <summary>
        /// Attempts to read the storage item.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <exception cref="StreamItemNotFoundException">if the item does not exist.</exception>
        /// <exception cref="StreamContainerNotFoundException">if the container for the item does not exist</exception>
        /// <exception cref="StreamingItemIntegrityException">when integrity check fails</exception>
        void ReadInto(Action<Stream> reader);

        /// <summary>
        /// Removes the item.
        /// </summary>
        void Delete();
        
    }
}