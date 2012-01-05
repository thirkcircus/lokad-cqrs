#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.IO;

namespace Lokad.Cqrs.AtomicStorage
{
    public sealed class DefaultAtomicStorageStrategy : IAtomicStorageStrategy
    {
        readonly string _folderForSingleton;
        readonly Func<Type, string> _nameForSingleton;
        readonly Func<Type, string> _folderForEntity;
        readonly Func<Type, object, string> _nameForEntity;
        readonly IAtomicStorageSerializer _serializer;

        public DefaultAtomicStorageStrategy(string folderForSingleton,
            Func<Type, string> nameForSingleton, Func<Type, string> folderForEntity,
            Func<Type, object, string> nameForEntity, IAtomicStorageSerializer serializer)
        {
            _serializer = serializer;
            _nameForEntity = nameForEntity;
            _folderForEntity = folderForEntity;
            _nameForSingleton = nameForSingleton;
            _folderForSingleton = folderForSingleton;
        }



        public string GetFolderForEntity(Type entityType, Type keyType)
        {
            if (keyType == typeof(unit))
            {
                return _folderForSingleton;
            }
            return _folderForEntity(entityType);
        }

        public string GetNameForEntity(Type entity, object key)
        {
            if (key is unit)
            {
                return _nameForSingleton(entity);
            }
            return _nameForEntity(entity, key);
        }


        public void Serialize<TEntity>(TEntity entity, Stream stream)
        {
            _serializer.Serialize(entity, stream);
        }

        public TEntity Deserialize<TEntity>(Stream stream)
        {
            return _serializer.Deserialize<TEntity>(stream);
        }

    }
}