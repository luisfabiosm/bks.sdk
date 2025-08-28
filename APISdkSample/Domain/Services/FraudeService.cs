using bks.sdk.Cache;
using bks.sdk.Observability.Logging;
using bks.sdk.Transactions;
using Domain.Core.Enums;
using Domain.Core.Interfaces.Domain;
using Domain.Core.Interfaces.Outbound;
using Domain.Core.Models;
using Domain.Core.Transactions;
using System.Text.Json;

namespace Domain.Services;

public class FraudeService : IFraudeService
{
    private readonly IContaRepository _contaRepository;
    private readonly IBKSLogger _logger;
    private readonly FraudeConfiguration _config;

    public FraudeService(
        IContaRepository contaRepository,
        IBKSLogger logger)
    {
        _contaRepository = contaRepository;
        _logger = logger;
        _config = new FraudeConfiguration();
    }

    public async ValueTask<AnaliseRisco> AnalisarTransacaoAsync(BaseTransaction transacao, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Info($"Iniciando análise de fraude para transação {transacao.CorrelationId}");

            var regrasValidadas = new List<RegrafValidada>();
            var motivos = new List<string>();
            var scoreRisco = 0;

            // Obter perfil de risco da conta
            int numeroConta = ExtrairNumeroContaTransacao(transacao);
            PerfilRisco? perfilRisco = null;

            if (numeroConta !=0)
            {
                perfilRisco = await ObterPerfilRiscoContaAsync(numeroConta, cancellationToken);
            }

            // Aplicar regras de análise
            await AplicarRegraDinheiroLavagem(transacao, regrasValidadas, motivos,  scoreRisco);
            await AplicarRegraComportamentoAtipico(transacao, perfilRisco, regrasValidadas, motivos,  scoreRisco);
            await AplicarRegraHorarioSuspeito(transacao, regrasValidadas, motivos,  scoreRisco);
            await AplicarRegraValorSuspeito(transacao, regrasValidadas, motivos,  scoreRisco);
            await AplicarRegraFrequenciaAlta(transacao, numeroConta, regrasValidadas, motivos,  scoreRisco);

            if (perfilRisco != null)
            {
                await AplicarRegraHistoricoSuspeito(perfilRisco, regrasValidadas, motivos,  scoreRisco);
            }

            // Determinar nível de risco
            var nivelRisco = DeterminarNivelRisco(scoreRisco);
            var isRisco = scoreRisco >= _config.LimiteScoreRisco;

            var analise = new AnaliseRisco
            {
                IsRisco = isRisco,
                NivelRisco = nivelRisco,
                ScoreRisco = scoreRisco,
                Motivos = motivos,
                RegrasValidadas = regrasValidadas,
                DataAnalise = DateTime.UtcNow,
                RecomendacaoAcao = GerarRecomendacaoAcao(scoreRisco, isRisco)
            };

            // Registrar evento suspeito se necessário
            if (isRisco && numeroConta !=0)
            {
                await RegistrarEventoSuspeitoAsync(
                    transacao.CorrelationId,
                    $"Score de risco: {scoreRisco} - {string.Join(", ", motivos.Take(3))}",
                    cancellationToken);
            }

            _logger.Info($"Análise de fraude concluída para transação {transacao.CorrelationId}: Score={scoreRisco}, Risco={isRisco}");

            return analise;
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro na análise de fraude para transação {transacao.CorrelationId}: {ex.Message}");

            // Em caso de erro, retornar análise conservadora (bloquear)
            return new AnaliseRisco
            {
                IsRisco = true,
                NivelRisco = TipoNivelRisco.Alto,
                ScoreRisco = 100,
                Motivos = ["Erro interno na análise de fraude"],
                RecomendacaoAcao = "Revisão manual necessária"
            };
        }
    }


    public async ValueTask<PerfilRisco> ObterPerfilRiscoContaAsync(int numeroConta, CancellationToken cancellationToken = default)
    {
        try
        {
            // Tentar obter do cache primeiro
            var cacheKey = $"perfil_risco_{numeroConta}";

      

            // Construir perfil baseado no histórico da conta
            var conta = await _contaRepository.GetByNumeroAsync(numeroConta, cancellationToken);
            if (conta == null)
            {
                return CriarPerfilRiscoPadrao(numeroConta);
            }

            var perfil = await ConstruirPerfilRisco(numeroConta, conta, cancellationToken);

            // Salvar no cache
            var json = JsonSerializer.Serialize(perfil);

            return perfil;
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao obter perfil de risco da conta {numeroConta}: {ex.Message}");
            return CriarPerfilRiscoPadrao(numeroConta);
        }
    }


    public async ValueTask RegistrarEventoSuspeitoAsync(string transacaoId, string motivo, CancellationToken cancellationToken = default)
    {
        try
        {
            var evento = new EventoSuspeito
            {
                Id = Guid.NewGuid().ToString(),
                TransacaoId = transacaoId,
                Motivo = motivo,
                DataEvento = DateTime.UtcNow,
                Severidade = DeterminarSeveridade(motivo)
            };

            // Registrar em cache para histórico recente
            var cacheKey = $"evento_suspeito_{evento.Id}";
            var json = JsonSerializer.Serialize(evento);

            _logger.Warn($"🚨 Evento suspeito registrado: {transacaoId} - {motivo}");

            // Aqui seria implementado o envio para sistema de compliance:
            // - Fila de análise manual
            // - Sistema de compliance
            // - Notificação para equipe de segurança
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao registrar evento suspeito: {ex.Message}");
        }
    }

    #region Regras de Análise de Fraude

    private async ValueTask AplicarRegraDinheiroLavagem(BaseTransaction transacao, List<RegrafValidada> regras, List<string> motivos,   int scoreRisco)
    {
        if (transacao is DebitoTransaction debito)
        {
            var violada = false;
            var detalhes = "";

            // Valores muito altos podem indicar lavagem
            if (debito.Valor >= 50000)
            {
                violada = true;
                detalhes = $"Valor alto: R$ {debito.Valor:F2}";
                scoreRisco += 25;
                motivos.Add("Transação de valor muito alto");
            }

            // Valores "redondos" podem ser suspeitos
            if (debito.Valor % 1000 == 0 && debito.Valor >= 10000)
            {
                violada = true;
                detalhes += $" | Valor redondo: R$ {debito.Valor:F2}";
                scoreRisco += 10;
                motivos.Add("Valor redondo suspeito");
            }

            regras.Add(new RegrafValidada
            {
                NomeRegra = "DINHEIRO_LAVAGEM",
                Violada = violada,
                PesoRisco = 8,
                Detalhes = detalhes
            });
        }
    }

    private async ValueTask AplicarRegraComportamentoAtipico( BaseTransaction transacao, PerfilRisco? perfil, List<RegrafValidada> regras, 
        List<string> motivos,  int scoreRisco)
    {
        var violada = false;
        var detalhes = "";

        if (perfil != null && transacao is DebitoTransaction debito)
        {
            // Verificar se o valor é muito acima do padrão
            var valorMedioHistorico = perfil.VolumeTransacoesUltimos30Dias / Math.Max(perfil.QuantidadeTransacoesUltimos30Dias, 1);

            if (debito.Valor > valorMedioHistorico * 5 && valorMedioHistorico > 0)
            {
                violada = true;
                detalhes = $"Valor 5x acima da média: R$ {debito.Valor:F2} vs R$ {valorMedioHistorico:F2}";
                scoreRisco += 20;
                motivos.Add("Valor muito acima do padrão histórico");
            }

            // Verificar frequência atípica
            if (perfil.QuantidadeTransacoesUltimos30Dias > 100)
            {
                violada = true;
                detalhes += $" | Alta frequência: {perfil.QuantidadeTransacoesUltimos30Dias} transações/mês";
                scoreRisco += 15;
                motivos.Add("Frequência de transações muito alta");
            }
        }

        regras.Add(new RegrafValidada
        {
            NomeRegra = "COMPORTAMENTO_ATIPICO",
            Violada = violada,
            PesoRisco = 7,
            Detalhes = detalhes
        });
    }

    private async ValueTask AplicarRegraHorarioSuspeito( BaseTransaction transacao, List<RegrafValidada> regras, List<string> motivos,  int scoreRisco)
    {
        var agora = DateTime.Now;
        var hora = agora.Hour;
        var violada = false;
        var detalhes = "";

        // Transações em horários atípicos (madrugada)
        if (hora >= 0 && hora <= 5)
        {
            violada = true;
            detalhes = $"Horário suspeito: {agora:HH:mm}";
            scoreRisco += 10;
            motivos.Add("Transação em horário atípico (madrugada)");
        }

        // Finais de semana à noite
        if ((agora.DayOfWeek == DayOfWeek.Saturday || agora.DayOfWeek == DayOfWeek.Sunday) &&
            (hora >= 22 || hora <= 6))
        {
            violada = true;
            detalhes += $" | Final de semana à noite: {agora:dddd HH:mm}";
            scoreRisco += 5;
            motivos.Add("Transação em final de semana à noite");
        }

        regras.Add(new RegrafValidada
        {
            NomeRegra = "HORARIO_SUSPEITO",
            Violada = violada,
            PesoRisco = 3,
            Detalhes = detalhes
        });
    }

    private async ValueTask AplicarRegraValorSuspeito( BaseTransaction transacao, List<RegrafValidada> regras, List<string> motivos,  int scoreRisco)
    {
        if (transacao is DebitoTransaction debito)
        {
            var violada = false;
            var detalhes = "";

            // Valores muito pequenos (possível teste)
            if (debito.Valor <= 1)
            {
                violada = true;
                detalhes = $"Valor muito pequeno: R$ {debito.Valor:F2}";
                scoreRisco += 15;
                motivos.Add("Valor muito pequeno (possível teste)");
            }

            // Valores em centavos específicos (podem indicar testes automatizados)
            var centavos = (debito.Valor * 100) % 100;
            if (centavos == 1 || centavos == 37 || centavos == 99)
            {
                violada = true;
                detalhes += $" | Centavos suspeitos: {centavos}";
                scoreRisco += 5;
                motivos.Add("Centavos em padrão suspeito");
            }

            regras.Add(new RegrafValidada
            {
                NomeRegra = "VALOR_SUSPEITO",
                Violada = violada,
                PesoRisco = 4,
                Detalhes = detalhes
            });
        }
    }

    private async ValueTask AplicarRegraFrequenciaAlta( BaseTransaction transacao, int numeroConta, List<RegrafValidada> regras, List<string> motivos, 
         int scoreRisco)
    {
        var violada = false;
        var detalhes = "";

        if (numeroConta !=0 )
        {
            // Verificar transações recentes no cache
            var cacheKey = $"freq_transacoes_{numeroConta}";

            var transacoesHoje = 1; // Transação atual
            var transacoesUltimaHora = 1;


            // Verificar limites de frequência
            if (transacoesHoje > _config.LimiteTransacoesPorDia)
            {
                violada = true;
                detalhes = $"Muitas transações hoje: {transacoesHoje}";
                scoreRisco += 20;
                motivos.Add($"Frequência muito alta: {transacoesHoje} transações hoje");
            }

            if (transacoesUltimaHora > _config.LimiteTransacoesPorHora)
            {
                violada = true;
                detalhes += $" | Última hora: {transacoesUltimaHora} transações";
                scoreRisco += 25;
                motivos.Add($"Frequência crítica: {transacoesUltimaHora} transações na última hora");
            }

            // Atualizar cache de frequência
            await AtualizarFrequenciaTransacoes(numeroConta);
        }

        regras.Add(new RegrafValidada
        {
            NomeRegra = "FREQUENCIA_ALTA",
            Violada = violada,
            PesoRisco = 9,
            Detalhes = detalhes
        });
    }

    private async ValueTask AplicarRegraHistoricoSuspeito( PerfilRisco perfil, List<RegrafValidada> regras, List<string> motivos, int scoreRisco)
    {
        var violada = false;
        var detalhes = "";

        // Verificar se a conta já tem histórico de eventos suspeitos
        if (perfil.EventosSuspeitos.Any())
        {
            var eventosRecentes = perfil.EventosSuspeitos
                .Where(e => e.DataEvento >= DateTime.UtcNow.AddDays(-30))
                .ToList();

            if (eventosRecentes.Count >= 3)
            {
                violada = true;
                detalhes = $"Histórico suspeito: {eventosRecentes.Count} eventos em 30 dias";
                scoreRisco += 30;
                motivos.Add("Conta com histórico suspeito recente");
            }
        }

        // Verificar se conta está bloqueada
        if (perfil.ContaBloqueada)
        {
            violada = true;
            detalhes += " | Conta bloqueada";
            scoreRisco += 50;
            motivos.Add("Tentativa de transação em conta bloqueada");
        }

        // Verificar score de comportamento baixo
        if (perfil.ScoreComportamento < 30)
        {
            violada = true;
            detalhes += $" | Score baixo: {perfil.ScoreComportamento}";
            scoreRisco += 15;
            motivos.Add("Score de comportamento muito baixo");
        }

        regras.Add(new RegrafValidada
        {
            NomeRegra = "HISTORICO_SUSPEITO",
            Violada = violada,
            PesoRisco = 10,
            Detalhes = detalhes
        });
    }

    #endregion

    #region Métodos Auxiliares

    private int ExtrairNumeroContaTransacao(BaseTransaction transacao)
    {
        return transacao switch
        {
            DebitoTransaction debito => debito.NumeroConta,
            // Adicionar outros tipos conforme necessário
            _ => 0
        };
    }

    private TipoNivelRisco DeterminarNivelRisco(int scoreRisco)
    {
        return scoreRisco switch
        {
            >= 80 => TipoNivelRisco.Critico,
            >= 60 => TipoNivelRisco.Alto,
            >= 30 => TipoNivelRisco.Medio,
            _ => TipoNivelRisco.Baixo
        };
    }

    private string GerarRecomendacaoAcao(int scoreRisco, bool isRisco)
    {
        if (!isRisco) return "Transação pode prosseguir normalmente";

        return scoreRisco switch
        {
            >= 80 => "Bloquear transação e acionar equipe de compliance",
            >= 60 => "Solicitar autenticação adicional do usuário",
            >= 40 => "Aplicar limite temporário na conta",
            _ => "Monitorar transações subsequentes"
        };
    }

    private TipoNivelRisco DeterminarSeveridade(string motivo)
    {
        if (motivo.Contains("bloqueada") || motivo.Contains("lavagem"))
            return TipoNivelRisco.Critico;

        if (motivo.Contains("frequência crítica") || motivo.Contains("valor muito alto"))
            return TipoNivelRisco.Alto;

        if (motivo.Contains("atípico") || motivo.Contains("suspeito"))
            return TipoNivelRisco.Medio;

        return TipoNivelRisco.Baixo;
    }

    private PerfilRisco CriarPerfilRiscoPadrao(int numeroConta)
    {
        return new PerfilRisco
        {
            NumeroConta = numeroConta,
            ScoreComportamento = 50, // Score neutro
            QuantidadeTransacoesUltimos30Dias = 0,
            VolumeTransacoesUltimos30Dias = 0m,
            PadroesIdentificados = ["CONTA_NOVA"],
            UltimaAtualizacao = DateTime.UtcNow,
            ContaBloqueada = false,
            EventosSuspeitos = new List<EventoSuspeito>()
        };
    }

    private async Task<PerfilRisco> ConstruirPerfilRisco(
        int numeroConta,
        Domain.Core.Models.Entities.Conta conta,
        CancellationToken cancellationToken)
    {
        // Analisar movimentações dos últimos 30 dias
        var movimentacoes30Dias = conta.Movimentacoes
            .Where(m => m.DataMovimentacao >= DateTime.UtcNow.AddDays(-30))
            .ToList();

        var quantidadeTransacoes = movimentacoes30Dias.Count;
        var volumeTransacoes = movimentacoes30Dias.Sum(m => m.Valor);

        // Identificar padrões
        var padroes = IdentificarPadroes(movimentacoes30Dias);

        // Calcular score de comportamento
        var score = CalcularScoreComportamento(conta, movimentacoes30Dias);

        // Buscar eventos suspeitos do cache
        var eventosSuspeitos = await BuscarEventosSuspeitos(numeroConta);

        return new PerfilRisco
        {
            NumeroConta = numeroConta,
            ScoreComportamento = score,
            QuantidadeTransacoesUltimos30Dias = quantidadeTransacoes,
            VolumeTransacoesUltimos30Dias = volumeTransacoes,
            PadroesIdentificados = padroes,
            UltimaAtualizacao = DateTime.UtcNow,
            ContaBloqueada = !conta.Ativa,
            EventosSuspeitos = eventosSuspeitos
        };
    }

    private List<string> IdentificarPadroes(List<Domain.Core.Models.Entities.MovimentacaoInfo> movimentacoes)
    {
        var padroes = new List<string>();

        if (!movimentacoes.Any())
        {
            padroes.Add("SEM_HISTORICO");
            return padroes;
        }

        // Analisar horários
        var movimentacoesMadrugada = movimentacoes.Count(m => m.DataMovimentacao.Hour <= 5);
        if (movimentacoesMadrugada > movimentacoes.Count * 0.3)
            padroes.Add("USO_MADRUGADA");

        // Analisar valores
        var valoresMedios = movimentacoes.Average(m => m.Valor);
        var valoresAltos = movimentacoes.Count(m => m.Valor > valoresMedios * 3);
        if (valoresAltos > 0)
            padroes.Add("VALORES_DISCREPANTES");

        // Analisar frequência
        var diasComMovimentacao = movimentacoes
            .GroupBy(m => m.DataMovimentacao.Date)
            .Count();

        if (diasComMovimentacao > 25) // Quase todos os dias
            padroes.Add("USO_DIARIO");
        else if (diasComMovimentacao < 5) // Uso esporádico
            padroes.Add("USO_ESPORADICO");

        // Analisar consistência de valores
        var desvioValores = CalcularDesvioValores(movimentacoes);
        if (desvioValores < 0.1) // Valores muito similares
            padroes.Add("VALORES_CONSISTENTES");
        else if (desvioValores > 2) // Valores muito variados
            padroes.Add("VALORES_VARIAVEIS");

        return padroes;
    }

    private int CalcularScoreComportamento(
        Domain.Core.Models.Entities.Conta conta,
        List<Domain.Core.Models.Entities.MovimentacaoInfo> movimentacoes30Dias)
    {
        var score = 50; // Score base

        // Pontuação por tempo de conta
        var idadeConta = DateTime.UtcNow - conta.DataCriacao;
        if (idadeConta.TotalDays > 365) score += 15; // Conta antiga
        else if (idadeConta.TotalDays < 30) score -= 10; // Conta muito nova

        // Pontuação por saldo
        if (conta.Saldo > 10000) score += 10;
        else if (conta.Saldo < 100) score -= 15;

        // Pontuação por padrão de uso
        if (movimentacoes30Dias.Count > 50) score -= 10; // Uso muito intenso
        else if (movimentacoes30Dias.Count > 10 && movimentacoes30Dias.Count <= 30) score += 10; // Uso normal
        else if (movimentacoes30Dias.Count == 0) score -= 20; // Sem uso

        // Pontuação por consistência
        if (movimentacoes30Dias.Any())
        {
            var desvio = CalcularDesvioValores(movimentacoes30Dias);
            if (desvio < 0.5) score += 5; // Comportamento consistente
            else if (desvio > 3) score -= 10; // Comportamento errático
        }

        return Math.Max(0, Math.Min(100, score));
    }

    private double CalcularDesvioValores(List<Domain.Core.Models.Entities.MovimentacaoInfo> movimentacoes)
    {
        if (movimentacoes.Count < 2) return 0;

        var valores = movimentacoes.Select(m => (double)m.Valor).ToArray();
        var media = valores.Average();
        var variancia = valores.Sum(v => Math.Pow(v - media, 2)) / valores.Length;
        var desviopadrao = Math.Sqrt(variancia);

        return media > 0 ? desviopadrao / media : 0; // Coeficiente de variação
    }

    private async Task<List<EventoSuspeito>> BuscarEventosSuspeitos(int numeroConta)
    {
        var eventos = new List<EventoSuspeito>();

        try
        {
            // Buscar eventos dos últimos 90 dias (implementação simplificada)
            for (int i = 0; i < 90; i++)
            {
                var cacheKey = $"evento_suspeito_conta_{numeroConta}_{DateTime.UtcNow.AddDays(-i):yyyyMMdd}";

            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao buscar eventos suspeitos: {ex.Message}");
        }

        return eventos.OrderByDescending(e => e.DataEvento).ToList();
    }

    private async Task AtualizarFrequenciaTransacoes(int numeroConta)
    {
        try
        {
            var cacheKey = $"freq_transacoes_{numeroConta}";

            var dados = new FrequenciaTransacoes();

            // Adicionar transação atual
            var agora = DateTime.UtcNow;
            dados.TransacoesRecentes.Add(agora);
            dados.TransacoesHoje.Add(agora);

            // Limpar dados antigos
            var limiteRecente = agora.AddHours(-24);
            dados.TransacoesRecentes = dados.TransacoesRecentes.Where(t => t >= limiteRecente).ToList();
            dados.TransacoesHoje = dados.TransacoesHoje.Where(t => t.Date == DateTime.Today).ToList();

            // Salvar no cache
            var json = JsonSerializer.Serialize(dados);
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao atualizar frequência de transações: {ex.Message}");
        }
    }

    #endregion
}
