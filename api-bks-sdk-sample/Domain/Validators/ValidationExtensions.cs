using bks.sdk.Common.Results;
using Domain.Core.Commands;
using Domain.Core.Transactions;

namespace Domain.Validators
{
    public static class ValidationExtensions
    {
        public static async Task<ValidationResult> ValidateAsync(
            this ProcessarCreditoCommand command,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken = default)
        {
            var validator = serviceProvider.GetRequiredService<ProcessarCreditoCommandValidator>();
            return await validator.ValidateAsync(command, cancellationToken);
        }

        public static async Task<ValidationResult> ValidateAsync(
            this DebitoTransaction transaction,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken = default)
        {
            var validator = serviceProvider.GetRequiredService<DebitoTransactionValidator>();
            return await validator.ValidateAsync(transaction, cancellationToken);
        }
    }
}
