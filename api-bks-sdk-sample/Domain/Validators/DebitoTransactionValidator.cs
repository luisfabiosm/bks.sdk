using bks.sdk.Observability.Logging;
using bks.sdk.Validation.Validators;
using Domain.Core.Ports.Outbound;
using Domain.Core.Transactions;

namespace Domain.Validators
{
    public class DebitoTransactionValidator : BaseValidator<DebitoTransaction>
    {
        private readonly IContaRepository _contaRepository;

        public DebitoTransactionValidator(
            IBKSLogger logger,
            IContaRepository contaRepository) : base(logger)
        {
            _contaRepository = contaRepository;
        }

        protected override void ConfigureRules()
        {
            // Validações síncronas usando as propriedades da transação
            AddRule(transaction => transaction.NumeroConta,
                numero => !(numero==0),
                "Número da conta de débito é obrigatório",
                "RequiredDebitAccount");

            AddRule(transaction => transaction.Valor,
                valor => valor > 0,
                "Valor deve ser maior que zero",
                "PositiveDebitValue");

            AddRule(transaction => transaction.Descricao,
                descricao => !string.IsNullOrWhiteSpace(descricao),
                "Descrição é obrigatória",
                "RequiredDebitDescription");

            // Usando as propriedades de validação da própria transação
            AddRule(transaction => transaction,
                t => t.IsValorValido,
                "Valor da transação é inválido",
                "ValidTransactionValue");

            AddRule(transaction => transaction,
                t => t.IsContaValida,
                "Dados da conta são inválidos",
                "ValidAccountData");

            AddRule(transaction => transaction,
                t => t.IsDescricaoValida,
                "Descrição da transação é inválida",
                "ValidTransactionDescription");

            // Validações assíncronas
            AddAsyncRule(transaction => transaction.NumeroConta,
                async numero => await ValidarContaParaDebito(numero),
                "Conta não pode ser debitada",
                "AccountCanBeDebited");

            AddAsyncRule(transaction => transaction,
                async t => await ValidarSaldoSuficiente(t),
                "Saldo insuficiente para a operação",
                "SufficientBalance");
        }

        protected override async Task<List<string>> ExecuteCustomValidationsAsync(
            DebitoTransaction instance,
            CancellationToken cancellationToken)
        {
            var errors = new List<string>();

            try
            {
                // Validação de horário para operações grandes
                if (instance.Valor > 10000 && !instance.IsHorarioComercial)
                {
                    errors.Add("Débitos acima de R$ 10.000,00 só podem ser realizados em horário comercial");
                }

                // Validação de frequência de transações
                var isAltaFrequencia = await VerificarAltaFrequenciaTransacoes(
                    instance.NumeroConta, cancellationToken);
                if (isAltaFrequencia && instance.Valor > 5000)
                {
                    errors.Add("Conta com alta frequência de transações tem limite reduzido para débitos");
                }

                // Validação específica para finais de semana
                if (DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
                {
                    if (instance.Valor > 2000)
                    {
                        errors.Add("Débitos acima de R$ 2.000,00 não são permitidos em finais de semana");
                    }
                }

                // Validação de padrão suspeito
                var isPadraoSuspeito = await DetectarPadraoSuspeito(instance, cancellationToken);
                if (isPadraoSuspeito)
                {
                    errors.Add("Padrão de transação requer aprovação manual");
                }

                Logger.Trace($"Validações customizadas de débito executadas. Erros: {errors.Count}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Erro durante validações customizadas de débito");
                errors.Add("Erro interno durante validação de débito");
            }

            return errors;
        }

        private async Task<bool> ValidarContaParaDebito(int numeroConta)
        {
            try
            {
                var conta = await _contaRepository.GetByNumeroAsync(numeroConta);
                return conta != null && conta.Ativa;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Erro ao validar conta para débito: {numeroConta}");
                return false;
            }
        }

        private async Task<bool> ValidarSaldoSuficiente(DebitoTransaction transaction)
        {
            try
            {
                var conta = await _contaRepository.GetByNumeroAsync(transaction.NumeroConta);

                if (conta == null)
                    return false;

                // Se permite saldo negativo, sempre válido
                if (transaction.PermitirSaldoNegativo)
                    return true;

                return conta.PodeSacar(transaction.Valor);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Erro ao validar saldo para débito: {transaction.NumeroConta}");
                return false;
            }
        }

        private async Task<bool> VerificarAltaFrequenciaTransacoes(int numeroConta, CancellationToken cancellationToken)
        {
            // Simulação - em implementação real consultaria histórico recente
            await Task.Delay(20, cancellationToken);

            try
            {
                var conta = await _contaRepository.GetByNumeroAsync(numeroConta, cancellationToken);
                if (conta == null) return false;

                // Considerar alta frequência se tem mais de 10 movimentações
                return conta.Movimentacoes.Count > 10;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> DetectarPadraoSuspeito(DebitoTransaction transaction, CancellationToken cancellationToken)
        {
            // Simulação de detecção de padrões suspeitos
            await Task.Delay(30, cancellationToken);

            // Valores redondos muito altos
            if (transaction.Valor >= 10000 && transaction.Valor % 1000 == 0)
            {
                return true;
            }

            // Descrições suspeitas
            var descricoesSuspeitas = new[] { "cash", "dinheiro", "emergencia", "urgente" };
            var descricaoLower = transaction.Descricao.ToLowerInvariant();

            if (descricoesSuspeitas.Any(s => descricaoLower.Contains(s)))
            {
                return true;
            }

            return false;
        }
    }

}
