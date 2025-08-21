using bks.sdk.Cache;
using bks.sdk.Common.Results;
using bks.sdk.Events;
using bks.sdk.Observability.Logging;
using bks.sdk.Observability.Tracing;
using bks.sdk.Transactions;
using Domain.Core.Interfaces.Domain;
using Domain.Core.Interfaces.Outbound;
using Domain.Core.Transactions;

namespace Domain.Processors
{
    public class DebitoProcessor : TransactionProcessor
    {
        private readonly IContaRepository _contaRepository;
        private readonly ILimiteService _limiteService;
        private readonly ICacheProvider _cacheProvider;
        private readonly INotificationService _notificationService;
        private readonly IAntiFraudeService _fraudeService;
        private readonly IBKSLogger? _logger;

        public DebitoProcessor(
                            IServiceProvider serviceProvider,
                            IBKSLogger logger,
                            IBKSTracer tracer,
                            IEventBroker eventBroker) : base(serviceProvider, logger, tracer, eventBroker)
        {
            _contaRepository = serviceProvider.GetRequiredService<IContaRepository>();
            _limiteService = serviceProvider.GetRequiredService<ILimiteService>(); 
            _cacheProvider = serviceProvider.GetRequiredService<ICacheProvider>(); 
            _notificationService = serviceProvider.GetRequiredService<INotificationService>(); 
            _fraudeService = serviceProvider.GetRequiredService<IAntiFraudeService>(); 
        }

        protected override async Task<ValidationResult> ValidateAsync(BaseTransaction transaction, CancellationToken cancellationToken)
        {
            var baseValidation = await base.ValidateAsync(transaction, cancellationToken);
            if (!baseValidation.IsValid)
                return baseValidation;

            var debito = (DebitoTransaction)transaction;
            var errors = new List<string>();

            // === VALIDAÇÕES BÁSICAS ===
            if (string.IsNullOrWhiteSpace(debito.NumeroContaDebito))
                errors.Add("Número da conta é obrigatório");

            if (debito.Valor <= 0)
                errors.Add("Valor deve ser maior que zero");

            if (debito.Valor > 500000) // Limite máximo débito
                errors.Add("Valor excede limite máximo de R$ 500.000");

            if (string.IsNullOrWhiteSpace(debito.Descricao))
                errors.Add("Descrição é obrigatória");

            if (debito.Descricao.Length > 200)
                errors.Add("Descrição muito longa (máximo 200 caracteres)");

            // === VALIDAÇÃO DE FORMATO ===
            if (!ValidarFormatoConta(debito.NumeroContaDebito))
                errors.Add("Formato da conta inválido (use: 12345-6)");

            // === VALIDAÇÕES ASSÍNCRONAS ===
            // Verificar se conta existe e está ativa
            var conta = await _contaRepository.GetByNumeroAsync(debito.NumeroContaDebito, cancellationToken);
            if (conta == null)
                errors.Add("Conta não encontrada");
            else if (!conta.Ativa)
                errors.Add("Conta inativa");

            // Validar limites diários
            if (conta != null)
            {
                var limiteValidacao = await _limiteService.ValidarLimiteDebitoAsync(
                    conta.Id, debito.Valor, cancellationToken);
                if (!limiteValidacao.IsValid)
                    errors.AddRange(limiteValidacao.Errors);
            }

            _logger?.Info($"Validação débito: {errors.Count} erros encontrados");

            return errors.Any()
                ? ValidationResult.Failure(errors)
                : ValidationResult.Success();
        }



    }

}