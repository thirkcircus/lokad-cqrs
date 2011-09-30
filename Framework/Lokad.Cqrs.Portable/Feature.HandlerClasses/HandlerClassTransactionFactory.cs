using System.Transactions;

namespace Lokad.Cqrs.Feature.HandlerClasses
{
    /// <summary>
    /// Function that creates transaction scope for the given envelope (use 
    /// <see cref="TransactionScopeOption.Suppress"/> for no transaction)
    /// </summary>
    /// <param name="envelope">The envelope (can be used for choosing to turn on transaction or not).</param>
    /// <returns>Transaction scope to be used for the provided envelope</returns>
    public delegate TransactionScope HandlerClassTransactionFactory(ImmutableEnvelope envelope);
}