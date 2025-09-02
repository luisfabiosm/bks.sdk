using bks.sdk.Common.Results;
using bks.sdk.Observability.Logging;
using bks.sdk.Validation.Validators;
using Domain.Core.Commands;
using Domain.Core.Entities;
using Domain.Core.Ports.Outbound;

namespace Domain.Validators
{
    public class ProcessarCreditoCommandValidator : BaseValidator<ProcessarCreditoCommand>
    {
        private readonly IContaRepository _contaRepository;

        public ProcessarCreditoCommandValidator(
            IBKSLogger logger,
            IContaRepository contaRepository) : base(logger)
        {
            _contaRepository = contaRepository;
        }

        protected override void ConfigureRules()
        {
            // Validações síncronas básicas
            AddRule(command => command.NumeroContaCredito,
                numero => !(numero==0),
                "Número da conta de crédito é obrigatório",
                "RequiredAccountNumber");


            AddRule(command => command.Valor,
                valor => valor > 0,
                "Valor deve ser maior que zero",
                "PositiveValue");

            AddRule(command => command.Valor,
                valor => valor <= 1_000_000,
                "Valor não pode exceder R$ 1.000.000,00",
                "MaxValue");

            AddRule(command => command.Descricao,
                descricao => !string.IsNullOrWhiteSpace(descricao),
                "Descrição é obrigatória",
                "RequiredDescription");

            AddRule(command => command.Descricao,
                descricao => descricao?.Length <= 200,
                "Descrição deve ter no máximo 200 caracteres",
                "MaxLengthDescription");

            // Validações assíncronas
            AddAsyncRule(command => command.NumeroContaCredito,
                async numero => await ContaExisteEAtiva(numero),
                "Conta não encontrada ou inativa",
                "AccountExistsAndActive");

            AddAsyncRule(command => command,
                async command => await ValidarLimitesDiarios(command),
                "Limite diário de créditos excedido",
                "DailyLimits");
        }

        protected override async Task<List<string>> ExecuteCustomValidationsAsync(
            ProcessarCreditoCommand instance,
            CancellationToken cancellationToken)
        {
            var errors = new List<string>();

            try
            {
                // Validação de horário para valores altos
                if (instance.Valor > 50000 && !IsHorarioComercial())
                {
                    errors.Add("Créditos acima de R$ 50.000,00 só podem ser processados em horário comercial");
                }

                // Validação de referência duplicada
                if (!string.IsNullOrWhiteSpace(instance.Referencia))
                {
                    var isDuplicada = await VerificarReferenciaDuplicada(instance.Referencia, cancellationToken);
                    if (isDuplicada)
                    {
                        errors.Add($"Referência '{instance.Referencia}' já foi utilizada");
                    }
                }

                // Validação específica para contas empresariais
                var conta = await _contaRepository.GetByNumeroAsync(instance.NumeroContaCredito, cancellationToken);
                if (conta != null && IsContaEmpresarial(conta.Titular))
                {
                    var validationResult = await ValidarCreditoEmpresarial(instance, conta, cancellationToken);
                    if (!validationResult.IsValid)
                    {
                        errors.AddRange(validationResult.Errors);
                    }
                }

                Logger.Trace($"Validações customizadas executadas para crédito. Erros encontrados: {errors.Count}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Erro durante validações customizadas de crédito");
                errors.Add("Erro interno durante validação");
            }

            return errors;
        }

        private async Task<bool> ContaExisteEAtiva(int numeroConta)
        {
            try
            {
                var conta = await _contaRepository.GetByNumeroAsync(numeroConta);
                return conta != null && conta.Ativa;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Erro ao verificar existência da conta: {numeroConta}");
                return false;
            }
        }

        private async Task<bool> ValidarLimitesDiarios(ProcessarCreditoCommand command)
        {
            // Simulação de validação de limite diário
            // Em uma implementação real, consultaria histórico de transações
            await Task.Delay(10);

            const decimal limiteDiario = 100_000m;
            return command.Valor <= limiteDiario;
        }

        private bool IsHorarioComercial()
        {
            var agora = DateTime.Now;
            return agora.Hour >= 8 && agora.Hour <= 18 &&
                   agora.DayOfWeek >= DayOfWeek.Monday && agora.DayOfWeek <= DayOfWeek.Friday;
        }

        private async Task<bool> VerificarReferenciaDuplicada(string referencia, CancellationToken cancellationToken)
        {
            // Simulação - em uma implementação real consultaria um cache ou BD
            await Task.Delay(5, cancellationToken);

            // Para este exemplo, considerar algumas referências como duplicadas
            var referenciasProibidas = new[] { "DUP-001", "TEST-DUP", "INVALID-REF" };
            return referenciasProibidas.Contains(referencia, StringComparer.OrdinalIgnoreCase);
        }

        private bool IsContaEmpresarial(string titular)
        {
            return titular.Contains("Ltda", StringComparison.OrdinalIgnoreCase) ||
                   titular.Contains("S/A", StringComparison.OrdinalIgnoreCase) ||
                   titular.Contains("ME", StringComparison.OrdinalIgnoreCase) ||
                   titular.Contains("Empresa", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<ValidationResult> ValidarCreditoEmpresarial(
            ProcessarCreditoCommand command,
            Conta conta,
            CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);

            var errors = new List<string>();

            // Limite maior para empresas
            if (command.Valor > 5_000_000)
            {
                errors.Add("Créditos empresariais não podem exceder R$ 5.000.000,00");
            }

            // Observações obrigatórias para valores altos
            if (command.Valor > 100_000 && string.IsNullOrWhiteSpace(command.Observacoes))
            {
                errors.Add("Observações são obrigatórias para créditos empresariais acima de R$ 100.000,00");
            }

            return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
        }
    }

}
