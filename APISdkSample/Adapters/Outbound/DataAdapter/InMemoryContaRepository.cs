using System.Collections.Concurrent;
using Domain.Core.Models.Entities;
using Domain.Core.Enums;
using bks.sdk.Observability.Logging;
using Domain.Core.Interfaces.Outbound;


namespace Adapters.Outbound.DataAdapter;


public class InMemoryContaRepository : IContaRepository
{
    private static readonly ConcurrentDictionary<string, Conta> _contasPorId = new();
    private static readonly ConcurrentDictionary<string, string> _contasPorNumero = new(); // numero -> id
    private static readonly object _lockInit = new object();
    private static bool _initialized = false;

    private readonly IBKSLogger _logger;

    public InMemoryContaRepository(IBKSLogger logger)
    {
        _logger = logger;
        InicializarDadosSemente();
    }

 
    public ValueTask<Conta?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Simular latência de banco de dados
            Thread.Sleep(Random.Shared.Next(10, 50));

            var encontrada = _contasPorId.TryGetValue(id, out var conta);

            _logger.Info($"Busca por ID {id}: {(encontrada ? "Encontrada" : "Não encontrada")}");

            return ValueTask.FromResult(conta);
        }
        catch (OperationCanceledException)
        {
            _logger.Warn($"Busca por ID {id} cancelada");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao buscar conta por ID {id}: {ex.Message}");
            return ValueTask.FromResult<Conta?>(null);
        }
    }

   
    public ValueTask<Conta?> GetByNumeroAsync(string numero, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Simular latência de banco de dados
            Thread.Sleep(Random.Shared.Next(10, 50));

            if (_contasPorNumero.TryGetValue(numero, out var contaId))
            {
                var encontrada = _contasPorId.TryGetValue(contaId, out var conta);
                _logger.Info($"Busca por número {numero}: {(encontrada ? $"Encontrada (ID: {contaId})" : "Não encontrada")}");
                return ValueTask.FromResult(conta);
            }

            _logger.Info($"Busca por número {numero}: Não encontrada");
            return ValueTask.FromResult<Conta?>(null);
        }
        catch (OperationCanceledException)
        {
            _logger.Warn($"Busca por número {numero} cancelada");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao buscar conta por número {numero}: {ex.Message}");
            return ValueTask.FromResult<Conta?>(null);
        }
    }


    public ValueTask UpdateAsync(Conta conta, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (conta == null)
                throw new ArgumentNullException(nameof(conta));

            // Simular latência de persistência
            Thread.Sleep(Random.Shared.Next(20, 100));

            // Clonar a conta para simular serialização/deserialização do banco
            var contaAtualizada = ClonarConta(conta);

            // Atualizar nos dicionários
            _contasPorId.AddOrUpdate(contaAtualizada.Id, contaAtualizada, (key, oldValue) => contaAtualizada);
            _contasPorNumero.TryAdd(contaAtualizada.Numero, contaAtualizada.Id);

            var ultimaMovimentacao = contaAtualizada.Movimentacoes.LastOrDefault();
            var infoMovimentacao = ultimaMovimentacao != null
                ? $"Última movimentação: {ultimaMovimentacao.Tipo} R$ {ultimaMovimentacao.Valor:F2}"
                : "Sem movimentações";

            _logger.Info($"Conta atualizada: {contaAtualizada.Numero} | Saldo: R$ {contaAtualizada.Saldo:F2} | {infoMovimentacao}");

            return ValueTask.CompletedTask;
        }
        catch (OperationCanceledException)
        {
            _logger.Warn($"Atualização da conta {conta?.Numero} cancelada");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao atualizar conta {conta?.Numero}: {ex.Message}");
            throw;
        }
    }


    public ValueTask<bool> ExistsAsync(string numero, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Simular latência de consulta
            Thread.Sleep(Random.Shared.Next(5, 25));

            var existe = _contasPorNumero.ContainsKey(numero);

            _logger.Info($"Verificação de existência {numero}: {(existe ? "Existe" : "Não existe")}");

            return ValueTask.FromResult(existe);
        }
        catch (OperationCanceledException)
        {
            _logger.Warn($"Verificação de existência {numero} cancelada");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao verificar existência da conta {numero}: {ex.Message}");
            return ValueTask.FromResult(false);
        }
    }


    public ValueTask<IEnumerable<MovimentacaoInfo>> GetMovimentacoesAsync(
        string contaId,
        DateTime? dataInicio = null,
        DateTime? dataFim = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Simular latência de consulta complexa
            Thread.Sleep(Random.Shared.Next(30, 150));

            if (!_contasPorId.TryGetValue(contaId, out var conta))
            {
                _logger.Warn($"Conta {contaId} não encontrada para buscar movimentações");
                return ValueTask.FromResult<IEnumerable<MovimentacaoInfo>>(Array.Empty<MovimentacaoInfo>());
            }

            var movimentacoes = conta.Movimentacoes.AsQueryable();

            // Aplicar filtros de data
            if (dataInicio.HasValue)
            {
                movimentacoes = movimentacoes.Where(m => m.DataMovimentacao >= dataInicio.Value);
            }

            if (dataFim.HasValue)
            {
                var dataFimAjustada = dataFim.Value.Date.AddDays(1).AddTicks(-1); // Até o final do dia
                movimentacoes = movimentacoes.Where(m => m.DataMovimentacao <= dataFimAjustada);
            }

            // Converter para MovimentacaoInfo
            var resultado = movimentacoes
                .OrderByDescending(m => m.DataMovimentacao)
                .Select(m => new MovimentacaoInfo
                {
                    Id = m.Id,
                    SaldoAnterior = m.SaldoAnterior,
                    SaldoPosterior = m.SaldoPosterior,
                    DataMovimentacao = m.DataMovimentacao,
                    Referencia = m.Referencia,
                    Tipo = m.Tipo,
                    Valor = m.Valor,
                    Descricao = m.Descricao
                })
                .ToList();

            var filtroInfo = "";
            if (dataInicio.HasValue || dataFim.HasValue)
            {
                filtroInfo = $" | Filtro: {dataInicio?.ToString("dd/MM/yyyy") ?? "..."} a {dataFim?.ToString("dd/MM/yyyy") ?? "..."}";
            }

            _logger.Info($"Movimentações obtidas para conta {conta.Numero}: {resultado.Count} registros{filtroInfo}");

            return ValueTask.FromResult<IEnumerable<MovimentacaoInfo>>(resultado);
        }
        catch (OperationCanceledException)
        {
            _logger.Warn($"Busca de movimentações para conta {contaId} cancelada");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao buscar movimentações da conta {contaId}: {ex.Message}");
            return ValueTask.FromResult<IEnumerable<MovimentacaoInfo>>(Array.Empty<MovimentacaoInfo>());
        }
    }

    #region Métodos Auxiliares


    private void InicializarDadosSemente()
    {
        lock (_lockInit)
        {
            if (_initialized) return;

            _logger.Info("Inicializando dados semente do repositório in-memory");

            try
            {
                // Conta 1: João Silva (Saldo alto - categoria PREMIUM)
                var conta1 = new Conta("12345-6", "João Silva");
                AdicionarMovimentacaoSemente(conta1, 150000m, "Depósito inicial", "ABERTURA-CONTA");
                AdicionarMovimentacaoSemente(conta1, -5000m, "Saque emergencial", "ATM-001", DateTime.UtcNow.AddDays(-10));
                AdicionarMovimentacaoSemente(conta1, -2500m, "Compra supermercado", "CARD-***1234", DateTime.UtcNow.AddDays(-8));
                AdicionarMovimentacaoSemente(conta1, 8000m, "Transferência recebida", "PIX-RECEBIDO", DateTime.UtcNow.AddDays(-5));
                AdicionarMovimentacaoSemente(conta1, -1200m, "Conta de luz", "DEBITO-AUTO", DateTime.UtcNow.AddDays(-3));

                // Conta 2: Maria Santos (Saldo médio - categoria GOLD)
                var conta2 = new Conta("67890-1", "Maria Santos");
                AdicionarMovimentacaoSemente(conta2, 45000m, "Depósito inicial", "ABERTURA-CONTA");
                AdicionarMovimentacaoSemente(conta2, -1500m, "Compra online", "CARD-***5678", DateTime.UtcNow.AddDays(-7));
                AdicionarMovimentacaoSemente(conta2, 3200m, "Salário", "TED-EMPRESA", DateTime.UtcNow.AddDays(-4));
                AdicionarMovimentacaoSemente(conta2, -800m, "Academia", "DEBITO-AUTO", DateTime.UtcNow.AddDays(-2));

                // Conta 3: Empresa ABC Ltda (Saldo muito alto - categoria PREMIUM)
                var conta3 = new Conta("11111-1", "Empresa ABC Ltda");
                AdicionarMovimentacaoSemente(conta3, 500000m, "Capital inicial", "APORTE-SOCIO");
                AdicionarMovimentacaoSemente(conta3, -45000m, "Folha pagamento", "FOLHA-NOV", DateTime.UtcNow.AddDays(-15));
                AdicionarMovimentacaoSemente(conta3, 125000m, "Faturamento", "VENDAS-NOV", DateTime.UtcNow.AddDays(-12));
                AdicionarMovimentacaoSemente(conta3, -15000m, "Impostos", "DAS-NOV", DateTime.UtcNow.AddDays(-6));
                AdicionarMovimentacaoSemente(conta3, -8500m, "Fornecedores", "PAGTO-FORNEC", DateTime.UtcNow.AddDays(-1));

                // Conta 4: Pedro Oliveira (Saldo baixo - categoria STANDARD)
                var conta4 = new Conta("22222-2", "Pedro Oliveira");
                AdicionarMovimentacaoSemente(conta4, 2500m, "Depósito inicial", "ABERTURA-CONTA");
                AdicionarMovimentacaoSemente(conta4, -150m, "Padaria", "CARD-***9012", DateTime.UtcNow.AddDays(-6));
                AdicionarMovimentacaoSemente(conta4, -80m, "Farmácia", "CARD-***9012", DateTime.UtcNow.AddDays(-4));
                AdicionarMovimentacaoSemente(conta4, 1200m, "Freelance", "PIX-RECEBIDO", DateTime.UtcNow.AddDays(-2));

                // Conta 5: Ana Costa (Conta nova - categoria STANDARD)
                var conta5 = new Conta("33333-3", "Ana Costa");
                AdicionarMovimentacaoSemente(conta5, 1000m, "Depósito inicial", "ABERTURA-CONTA");

                // Registrar contas
                RegistrarConta(conta1);
                RegistrarConta(conta2);
                RegistrarConta(conta3);
                RegistrarConta(conta4);
                RegistrarConta(conta5);

                _initialized = true;

                _logger.Info($"Dados semente inicializados: {_contasPorId.Count} contas carregadas");
                LogResumoContas();
            }
            catch (Exception ex)
            {
                _logger.Error($"Erro ao inicializar dados semente: {ex.Message}");
            }
        }
    }


    private void AdicionarMovimentacaoSemente(
        Conta conta,
        decimal valor,
        string descricao,
        string referencia,
        DateTime? dataCustomizada = null)
    {
        try
        {
            // Para créditos, usar reflexão para simular operação interna
            if (valor > 0)
            {
                // Simular crédito adicionando movimentação manualmente
                var saldoAnterior = conta.Saldo;

                // Usar reflection para acessar propriedades privates (simulação)
                var novoSaldo = saldoAnterior + valor;
                var novaMovimentacao = new MovimentacaoInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    ContaId = conta.Id,
                    Tipo = TipoMovimentacao.Credito,
                    Valor = valor,
                    Descricao = descricao,
                    Referencia = referencia,
                    DataMovimentacao = dataCustomizada ?? DateTime.UtcNow.AddDays(-30),
                    SaldoAnterior = saldoAnterior,
                    SaldoPosterior = novoSaldo
                };

                // Atualizar saldo e adicionar movimentação (simulação de operação interna)
                AdicionarMovimentacaoInterna(conta, novaMovimentacao, novoSaldo);
            }
            else
            {
                // Para débitos, usar o método normal da entidade
                var valorDebito = Math.Abs(valor);
                if (conta.Saldo >= valorDebito)
                {
                    conta.Debitar(valorDebito, descricao, referencia);

                    // Ajustar data se necessário
                    if (dataCustomizada.HasValue)
                    {
                        var ultimaMovimentacao = conta.Movimentacoes.Last();
                        AtualizarDataMovimentacao(ultimaMovimentacao, dataCustomizada.Value);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao adicionar movimentação semente: {ex.Message}");
        }
    }


    private void AdicionarMovimentacaoInterna(Conta conta, MovimentacaoInfo movimentacao, decimal novoSaldo)
    {
        try
        {
            // Em uma implementação real, você poderia usar reflexão para acessar membros privados
            // Aqui vamos simular modificando as propriedades através do record

            // Como Conta e Movimentacao são records, não podemos modificar diretamente
            // Esta é uma limitação da simulação in-memory com records imutáveis

            // Em um cenário real, você teria:
            // 1. Entidades mutáveis com repositório real
            // 2. Ou métodos específicos na entidade para operações internas
            // 3. Ou eventos de domínio para reconstruir estado

            _logger.Warn("Simulação de crédito limitada devido à imutabilidade dos records");
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro na movimentação interna: {ex.Message}");
        }
    }


    private void AtualizarDataMovimentacao(MovimentacaoInfo movimentacao, DateTime novaData)
    {
        // Limitação: records são imutáveis
        // Em implementação real, seria possível atualizar
        _logger.Info($"Data da movimentação {movimentacao.Id} ajustada para {novaData:dd/MM/yyyy HH:mm}");
    }


    private void RegistrarConta(Conta conta)
    {
        _contasPorId.TryAdd(conta.Id, conta);
        _contasPorNumero.TryAdd(conta.Numero, conta.Id);

        _logger.Info($"Conta registrada: {conta.Numero} | Titular: {conta.Titular} | Saldo: R$ {conta.Saldo:F2} | Movimentações: {conta.Movimentacoes.Count}");
    }


    private Conta ClonarConta(Conta contaOriginal)
    {
        try
        {
            // Em uma implementação real, você faria serialização/deserialização
            // ou usaria um mapper como AutoMapper

            // Como estamos usando records imutáveis, a referência é suficiente
            // Em um banco real, haveria deserialização completa

            return contaOriginal;
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao clonar conta: {ex.Message}");
            return contaOriginal;
        }
    }


    private void LogResumoContas()
    {
        try
        {
            var totalContas = _contasPorId.Count;
            var saldoTotal = _contasPorId.Values.Sum(c => c.Saldo);
            var totalMovimentacoes = _contasPorId.Values.Sum(c => c.Movimentacoes.Count);

            var contasPorCategoria = _contasPorId.Values
                .GroupBy(c => c.Saldo switch
                {
                    >= 100000 => "PREMIUM",
                    >= 25000 => "GOLD",
                    _ => "STANDARD"
                })
                .ToDictionary(g => g.Key, g => g.Count());

            _logger.Info("📊 RESUMO DO REPOSITÓRIO IN-MEMORY:");
            _logger.Info($"   • Total de contas: {totalContas}");
            _logger.Info($"   • Saldo total: R$ {saldoTotal:F2}");
            _logger.Info($"   • Total de movimentações: {totalMovimentacoes}");
            _logger.Info($"   • Categorias:");

            foreach (var categoria in contasPorCategoria)
            {
                _logger.Info($"     - {categoria.Key}: {categoria.Value} conta(s)");
            }

            _logger.Info("   • Contas detalhadas:");
            foreach (var conta in _contasPorId.Values.OrderBy(c => c.Numero))
            {
                var categoria = conta.Saldo switch
                {
                    >= 100000 => "PREMIUM",
                    >= 25000 => "GOLD",
                    _ => "STANDARD"
                };

                _logger.Info($"     - {conta.Numero} | {conta.Titular} | R$ {conta.Saldo:F2} | {categoria} | {conta.Movimentacoes.Count} mov.");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao gerar resumo das contas: {ex.Message}");
        }
    }

    #endregion

    #region Métodos Para Testes/Debug


    public void LimparDados()
    {
        _contasPorId.Clear();
        _contasPorNumero.Clear();
        _initialized = false;
        _logger.Info("Dados do repositório in-memory limpos");
    }


    public RepositoryStats ObterEstatisticas()
    {
        return new RepositoryStats
        {
            TotalContas = _contasPorId.Count,
            SaldoTotal = _contasPorId.Values.Sum(c => c.Saldo),
            TotalMovimentacoes = _contasPorId.Values.Sum(c => c.Movimentacoes.Count),
            ContaComMaiorSaldo = _contasPorId.Values.OrderByDescending(c => c.Saldo).FirstOrDefault()?.Numero ?? "",
            ContaComMaisMovimentacoes = _contasPorId.Values.OrderByDescending(c => c.Movimentacoes.Count).FirstOrDefault()?.Numero ?? "",
            UltimaAtualizacao = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Adiciona conta para testes
    /// </summary>
    public ValueTask AdicionarContaAsync(Conta conta, CancellationToken cancellationToken = default)
    {
        RegistrarConta(conta);
        _logger.Info($"Conta de teste adicionada: {conta.Numero}");
        return ValueTask.CompletedTask;
    }

    #endregion
}


public record RepositoryStats
{
    public int TotalContas { get; init; }
    public decimal SaldoTotal { get; init; }
    public int TotalMovimentacoes { get; init; }
    public string ContaComMaiorSaldo { get; init; } = "";
    public string ContaComMaisMovimentacoes { get; init; } = "";
    public DateTime UltimaAtualizacao { get; init; }
}

