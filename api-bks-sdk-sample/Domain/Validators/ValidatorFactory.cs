using bks.sdk.Observability.Logging;
using Domain.Core.Ports.Outbound;

namespace Domain.Validators
{
    public static class ValidatorFactory
    {
        public static ProcessarCreditoCommandValidator CreateCreditoValidator(
            IBKSLogger logger,
            IContaRepository contaRepository)
        {
            return new ProcessarCreditoCommandValidator(logger, contaRepository);
        }

        public static DebitoTransactionValidator CreateDebitoValidator(
            IBKSLogger logger,
            IContaRepository contaRepository)
        {
            return new DebitoTransactionValidator(logger, contaRepository);
        }
    }

}
