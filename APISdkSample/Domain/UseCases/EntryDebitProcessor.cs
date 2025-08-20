using bks.sdk.Common.Results;
using bks.sdk.Transactions;
using Domain.Core.Transactions;

namespace Domain.UseCases
{
    public class EntryDebitProcessor : TransactionProcessor
    {
        private readonly IContaRepository _contaRepository;
        private readonly ILogger<EntryDebitProcessor> _logger;

        public EntryDebitProcessor(
            IContaRepository contaRepository,
            ILogger<EntryDebitProcessor> logger)
        {
            _contaRepository = contaRepository;
            _logger = logger;
        }


        protected override async Task<Result> ProcessAsync(
        BaseTransaction transaction,
        CancellationToken cancellationToken)
        {
            if (transaction is not EntryDebitTransaction _transaction)
                return Result.Failure("Tipo de transação inválido para débito");

            // Validações de negócio
            if (_transaction.Value <= 0)
                return Result.Failure("Valor deve ser maior que zero");

            if (string.IsNullOrWhiteSpace(_transaction.AccountNumber))
                return Result.Failure("Número da conta é obrigatório");

            try
            {
                // Buscar conta
                var conta = await _contaRepository.GetByNumeroAsync(
                    _transaction.AccountNumber, cancellationToken);

                if (conta == null)
                    return Result.Failure("Conta não encontrada");

                // Verificar saldo
                if (conta.Saldo < _transaction.Value)
                    return Result.Failure("Saldo insuficiente");

                // Executar débito
                conta.Debitar(_transaction.Value, _transaction.Detail);

                // Persistir alterações
                await _contaRepository.UpdateAsync(conta, cancellationToken);

                _logger.LogInformation("Débito realizado com sucesso: {Valor} da conta {Conta}",
                    _transaction.Value, _transaction.AccountNumber);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar débito");
                return Result.Failure("Erro interno ao processar débito");
            }
        }
    }
}
