namespace Lokad.Cqrs.AtomicStorage
{
    public interface IAtomicStorageFactory 
    {
        IAtomicWriter<TKey,TEntity> GetEntityWriter<TKey,TEntity>();
        IAtomicReader<TKey,TEntity> GetEntityReader<TKey,TEntity>();
    }
}