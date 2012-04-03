using System;
using System.IO;
using Lokad.Cqrs;
using Lokad.Cqrs.AtomicStorage;
using ServiceStack.Text;

namespace Sample.Wires
{
    public sealed class ProjectionStrategy : IDocumentStrategy
    {
        public string GetEntityBucket<T>()
        {
            var type = typeof(T);
            if (type == typeof(unit))
            {
                return "sample-ui";
            }
            return "sample-ui/" + type.Name.ToLowerInvariant();
        }

        public string GetEntityLocation(Type entity, object key)
        {
            if (key is unit)
                return entity.Name.ToLowerInvariant() + ".txt";
            if (key is IIdentity)
                return IdentityConvert.ToStream((IIdentity)key) + ".txt";
            return key.ToString().ToLowerInvariant() + ".txt";
        }


        public void Serialize<TEntity>(TEntity entity, Stream stream)
        {
            JsonSerializer.SerializeToStream(entity, stream);
        }

        public TEntity Deserialize<TEntity>(Stream stream)
        {
            return JsonSerializer.DeserializeFromStream<TEntity>(stream);
        }
    }

    public sealed class DocumentStrategy : IDocumentStrategy
    {
        public string GetEntityBucket<T>()
        {
            var type = typeof(T);
            if (type == typeof(unit))
            {
                return "sample-data";
            }
            return "sample-data/" + type.Name.ToLowerInvariant();
        }

        public string GetEntityLocation(Type entity, object key)
        {
            if (key is unit)
                return entity.Name.ToLowerInvariant() + ".txt";
            if (key is IIdentity)
                return IdentityConvert.ToStream((IIdentity)key) + ".txt";
            return key.ToString().ToLowerInvariant() + ".txt";
        }

        public void Serialize<TEntity>(TEntity entity, Stream stream)
        {
            JsonSerializer.SerializeToStream(entity, stream);
        }

        public TEntity Deserialize<TEntity>(Stream stream)
        {
            return JsonSerializer.DeserializeFromStream<TEntity>(stream);
        }
    }
}