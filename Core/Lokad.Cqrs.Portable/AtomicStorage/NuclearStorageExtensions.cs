using System;

namespace Lokad.Cqrs.AtomicStorage
{
    public static class NuclearStorageExtensions
    {
        public static TSingleton UpdateSingletonEnforcingNew<TSingleton>(this NuclearStorage storage, Action<TSingleton> update)
            where TSingleton : new()
        {
            return storage.Factory.GetEntityWriter<unit,TSingleton>().UpdateEnforcingNew(unit.it,update);
        }
        public static TEntity UpdateEntityEnforcingNew<TEntity>(this NuclearStorage storage, object key, Action<TEntity> update)
            where TEntity : new()
        {
            return storage.Factory.GetEntityWriter<object, TEntity>().UpdateEnforcingNew(key, update);
        }
    }

    
}