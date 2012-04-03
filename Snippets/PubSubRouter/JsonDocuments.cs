using System;
using System.IO;
using Lokad.Cqrs.AtomicStorage;
using ServiceStack.Text;

namespace Snippets.PubSubRouter
{
    public sealed class JsonDocuments : IDocumentStrategy
    {
        public string GetEntityBucket<TEntity>()
        {
            return typeof(TEntity).Name;
        }

        public string GetEntityLocation(Type entity, object key)
        {
            return key.ToString();
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