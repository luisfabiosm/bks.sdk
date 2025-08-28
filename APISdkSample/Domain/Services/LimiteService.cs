using bks.sdk.Cache;
using bks.sdk.Common.Results;
using bks.sdk.Observability.Logging;
using Domain.Core.Enums;
using Domain.Core.Interfaces.Domain;
using Domain.Core.Interfaces.Outbound;
using Domain.Core.Models;
using Domain.Core.Models.Entities;
using System.Text.Json;

namespace Domain.Services
{
    public class LimiteService : ILimiteService
    {

        private readonly IContaRepository _contaRepository;
        private readonly IBKSLogger _logger;
        private readonly LimiteConfiguration _config;

        public LimiteService(
         
            IContaRepository contaRepository,
            IBKSLogger logger)
        {
            _contaRepository = contaRepository;
            _logger = logger;
            _config = new LimiteConfiguration();
                
        }

        public async ValueTask<ValidationResult> ValidarLimiteDebitoAsync(
            int numeroConta,
            decimal valor,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Info($"Validando limite de débito para conta {numeroConta}: R$ {valor:F2}");

                // Obter limites atuais da conta
                var limites = await ObterLimitesContaAsync(numeroConta, cancellationToken);

                var errors = new List<string>();

                // Validar limite diário
                if (limites.DisponibilidadeDebitoDiario < valor)
                {
                    errors.Add($"Limite diário excedido. Disponível: R$ {limites.DisponibilidadeDebitoDiario:F2}, Solicitado: R$ {valor:F2}");
                    _logger.Warn($"Limite diário excedido para conta {numeroConta}: Disponível: {limites.DisponibilidadeDebitoDiario:C}, Solicitado: {valor:C}");
                }

                // Validar limite mensal
                if (limites.DisponibilidadeDebitoMensal < valor)
                {
                    errors.Add($"Limite mensal excedido. Disponível: R$ {limites.DisponibilidadeDebitoMensal:F2}, Solicitado: R$ {valor:F2}");
                    _logger.Warn($"Limite mensal excedido para conta {numeroConta}: Disponível: {limites.DisponibilidadeDebitoMensal:C}, Solicitado: {valor:C}");
                }

                // Validar limite por transação
                if (valor > _config.LimiteMaximoPorTransacao)
                {
                    errors.Add($"Valor por transação excedido. Máximo: R$ {_config.LimiteMaximoPorTransacao:F2}, Solicitado: R$ {valor:F2}");
                    _logger.Warn($"Valor por transação excedido para conta {numeroConta}: Máximo: {_config.LimiteMaximoPorTransacao:C}, Solicitado: {valor:C}");
                }

                if (errors.Any())
                {
                    return ValidationResult.Failure(errors);
                }

                _logger.Info($"Validação de limite aprovada para conta {numeroConta}: R$ {valor:F2}");
                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.Error($"Erro na validação de limite para conta {numeroConta}: {ex.Message}");
                return ValidationResult.Failure("Erro interno na validação de limites");
            }
        }

        public async ValueTask AtualizarLimiteUtilizadoAsync(
            int numeroConta,
            decimal valor,
            TipoLimite tipo,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Info($"Atualizando limite utilizado para conta {numeroConta}: {tipo} = R$ {valor:F2}");

                var limites = await ObterLimitesContaAsync(numeroConta, cancellationToken);

                // Atualizar conforme o tipo de limite
                var limitesAtualizados = tipo switch
                {
                    TipoLimite.DebitoDiario => limites with
                    {
                        UtilizadoDebitoDiario = limites.UtilizadoDebitoDiario + valor,
                        UltimaAtualizacao = DateTime.UtcNow
                    },
                    TipoLimite.DebitoMensal => limites with
                    {
                        UtilizadoDebitoMensal = limites.UtilizadoDebitoMensal + valor,
                        UltimaAtualizacao = DateTime.UtcNow
                    },
                    TipoLimite.TransferenciaDiaria => limites with
                    {
                        UtilizadoTransferenciaDiaria = limites.UtilizadoTransferenciaDiaria + valor,
                        UltimaAtualizacao = DateTime.UtcNow
                    },
                    _ => limites
                };

                // Salvar no cache
                var cacheKey = $"limites_{numeroConta}";
                var json = JsonSerializer.Serialize(limitesAtualizados);

                _logger.Info($"Limite atualizado com sucesso para conta {numeroConta}: {tipo}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Erro ao atualizar limite para conta {numeroConta}: {ex.Message}");
            }
        }

        public async ValueTask<LimiteInfo> ObterLimitesContaAsync(
            int numeroConta,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Tentar obter do cache primeiro
                var cacheKey = $"limites_{numeroConta}";

      

                // Buscar conta para determinar categoria de limites
                var conta = await _contaRepository.GetByNumeroAsync(numeroConta, cancellationToken);
                if (conta == null)
                {
                    throw new InvalidOperationException($"Conta {numeroConta} não encontrada");
                }

                // Criar limites baseados na categoria da conta
                var limites = CriarLimitesIniciais(numeroConta, conta);

    

                // Salvar no cache
                var json = JsonSerializer.Serialize(limites);

                return limites;
            }
            catch (Exception ex)
            {
                _logger.Error($"Erro ao obter limites da conta {numeroConta}: {ex.Message}");

                // Retornar limites padrão em caso de erro
                return CriarLimitesIniciais(numeroConta, null);
            }
        }

        public async ValueTask<bool> RedefinirLimitesAsync(
            int numeroConta,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = $"limites_{numeroConta}";


                _logger.Info($"Limites redefinidos para conta {numeroConta}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Erro ao redefinir limites da conta {numeroConta}: {ex.Message}");
                return false;
            }
        }

        #region Métodos Privados

        private LimiteInfo CriarLimitesIniciais(int numeroConta, Conta? conta)
        {
            // Determinar categoria da conta baseado no saldo ou outros critérios
            var categoria = DeterminarCategoriaLimite(conta);

            var agora = DateTime.UtcNow;

            return categoria switch
            {
                "PREMIUM" => new LimiteInfo
                {
                    NumeroConta = numeroConta,
                    LimiteDebitoDiario = 50000m,
                    LimiteDebitoMensal = 500000m,
                    LimiteTransferenciaDiaria = 100000m,
                    UtilizadoDebitoDiario = 0m,
                    UtilizadoDebitoMensal = 0m,
                    UtilizadoTransferenciaDiaria = 0m,
                    UltimaAtualizacao = agora,
                    DataResetDiario = agora.Date.AddDays(1),
                    DataResetMensal = new DateTime(agora.Year, agora.Month, 1).AddMonths(1)
                },
                "GOLD" => new LimiteInfo
                {
                    NumeroConta = numeroConta,
                    LimiteDebitoDiario = 20000m,
                    LimiteDebitoMensal = 200000m,
                    LimiteTransferenciaDiaria = 50000m,
                    UtilizadoDebitoDiario = 0m,
                    UtilizadoDebitoMensal = 0m,
                    UtilizadoTransferenciaDiaria = 0m,
                    UltimaAtualizacao = agora,
                    DataResetDiario = agora.Date.AddDays(1),
                    DataResetMensal = new DateTime(agora.Year, agora.Month, 1).AddMonths(1)
                },
                _ => new LimiteInfo // STANDARD
                {
                    NumeroConta = numeroConta,
                    LimiteDebitoDiario = 5000m,
                    LimiteDebitoMensal = 50000m,
                    LimiteTransferenciaDiaria = 10000m,
                    UtilizadoDebitoDiario = 0m,
                    UtilizadoDebitoMensal = 0m,
                    UtilizadoTransferenciaDiaria = 0m,
                    UltimaAtualizacao = agora,
                    DataResetDiario = agora.Date.AddDays(1),
                    DataResetMensal = new DateTime(agora.Year, agora.Month, 1).AddMonths(1)
                }
            };
        }

        private string DeterminarCategoriaLimite(Conta? conta)
        {
            if (conta == null) return "STANDARD";

            return conta.Saldo switch
            {
                >= 100000 => "PREMIUM",
                >= 25000 => "GOLD",
                _ => "STANDARD"
            };
        }

        private bool PrecisaReset(LimiteInfo limites)
        {
            var agora = DateTime.UtcNow;
            return agora >= limites.DataResetDiario || agora >= limites.DataResetMensal;
        }

        private LimiteInfo AplicarResetLimites(LimiteInfo limitesNovos, LimiteInfo limitesAnteriores)
        {
            var agora = DateTime.UtcNow;

            // Reset diário
            if (agora >= limitesAnteriores.DataResetDiario)
            {
                limitesNovos = limitesNovos with
                {
                    UtilizadoDebitoDiario = 0m,
                    UtilizadoTransferenciaDiaria = 0m,
                    DataResetDiario = agora.Date.AddDays(1)
                };
            }
            else
            {
                // Manter valores utilizados do cache
                limitesNovos = limitesNovos with
                {
                    UtilizadoDebitoDiario = limitesAnteriores.UtilizadoDebitoDiario,
                    UtilizadoTransferenciaDiaria = limitesAnteriores.UtilizadoTransferenciaDiaria
                };
            }

            // Reset mensal
            if (agora >= limitesAnteriores.DataResetMensal)
            {
                limitesNovos = limitesNovos with
                {
                    UtilizadoDebitoMensal = 0m,
                    DataResetMensal = new DateTime(agora.Year, agora.Month, 1).AddMonths(1)
                };
            }
            else
            {
                // Manter valor utilizado do cache
                limitesNovos = limitesNovos with
                {
                    UtilizadoDebitoMensal = limitesAnteriores.UtilizadoDebitoMensal
                };
            }

            return limitesNovos;
        }

        #endregion
    }
}
