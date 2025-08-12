using bks.sdk.Transactions.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Transactions.Handlres
{
    public interface ITransactionValidator<in TTransaction>
    {
        /// <summary>
        /// Valida a transação
        /// </summary>
        ValueTask<ValidationResult> ValidateAsync(TTransaction transaction, TransactionContext context, CancellationToken cancellationToken = default);
    }
}
