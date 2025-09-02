using Domain.Core.Entities;
using Domain.Core.Ports.Outbound;
using System.Collections.Concurrent;

namespace Adapters.Outbound.DataAdapter;


public class InMemoryContaRepository : IContaRepository
{
    private static readonly ConcurrentDictionary<string, Conta> _contas = new();
    private static readonly ConcurrentDictionary<int, Conta> _contasPorNumero = new();
    private readonly ILogger<InMemoryContaRepository> _logger;

    static InMemoryContaRepository()
    {
        // Inicializar com algumas contas de exemplo para testes
        InicializarContasDeExemplo();
    }

    public InMemoryContaRepository(ILogger<InMemoryContaRepository> logger)
    {
        _logger = logger;
    }

    public async Task<Conta?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Task.Delay(50, cancellationToken); // Simula latência de banco

        var sucesso = _contas.TryGetValue(id, out var conta);

        _logger.LogInformation("Busca conta por ID: {Id} - Encontrada: {Encontrada}", id, sucesso);

        return conta;
    }

    public async Task<Conta?> GetByNumeroAsync(int numero, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Task.Delay(50, cancellationToken); // Simula latência de banco

        var sucesso = _contasPorNumero.TryGetValue(numero, out var conta);

        _logger.LogInformation("Busca conta por número: {Numero} - Encontrada: {Encontrada}", numero, sucesso);

        return conta;
    }

    public async Task<IEnumerable<Conta>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Task.Delay(100, cancellationToken);

        var contas = _contas.Values.ToList();

        _logger.LogInformation("Busca todas as contas - Total: {Total}", contas.Count);

        return contas;
    }

    public async Task<IEnumerable<Conta>> GetByTitularAsync(string titular, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Task.Delay(75, cancellationToken);

        var contas = _contas.Values
            .Where(c => c.Titular.Contains(titular, StringComparison.OrdinalIgnoreCase))
            .ToList();

        _logger.LogInformation("Busca contas por titular: {Titular} - Encontradas: {Total}", titular, contas.Count);

        return contas;
    }

    public async Task CreateAsync(Conta conta, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (conta == null)
            throw new ArgumentNullException(nameof(conta));

        // Verificar se já existe uma conta com o mesmo número
        if (await ExistsAsync(conta.Numero, cancellationToken))
            throw new InvalidOperationException($"Já existe uma conta com o número: {conta.Numero}");

        await Task.Delay(100, cancellationToken); // Simula operação de criação

        _contas.TryAdd(conta.Id, conta);
        _contasPorNumero.TryAdd(conta.Numero, conta);

        _logger.LogInformation("Conta criada: {Id} - Número: {Numero} - Titular: {Titular}",
            conta.Id, conta.Numero, conta.Titular);
    }

    public async Task UpdateAsync(Conta conta, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (conta == null)
            throw new ArgumentNullException(nameof(conta));

        await Task.Delay(75, cancellationToken); // Simula operação de atualização

        _contas.AddOrUpdate(conta.Id, conta, (key, oldValue) => conta);
        _contasPorNumero.AddOrUpdate(conta.Numero, conta, (key, oldValue) => conta);

        _logger.LogInformation("Conta atualizada: {Id} - Número: {Numero} - Saldo: R$ {Saldo:F2}",
            conta.Id, conta.Numero, conta.Saldo);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var conta = await GetByIdAsync(id, cancellationToken);
        if (conta == null)
            throw new InvalidOperationException($"Conta não encontrada: {id}");

        await Task.Delay(50, cancellationToken);

        _contas.TryRemove(id, out _);
        _contasPorNumero.TryRemove(conta.Numero, out _);

        _logger.LogInformation("Conta removida: {Id} - Número: {Numero}", id, conta.Numero);
    }

    public async Task<bool> ExistsAsync(int numero, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Task.Delay(25, cancellationToken);

        var existe = _contasPorNumero.ContainsKey(numero);

        _logger.LogDebug("Verificação de existência - Número: {Numero} - Existe: {Existe}", numero, existe);

        return existe;
    }

    public async Task<IEnumerable<Movimentacao>> GetMovimentacoesAsync(string contaId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var conta = await GetByIdAsync(contaId, cancellationToken);

        if (conta == null)
            return Enumerable.Empty<Movimentacao>();

        _logger.LogInformation("Busca movimentações da conta: {ContaId} - Total: {Total}",
            contaId, conta.Movimentacoes.Count);

        return conta.Movimentacoes.OrderByDescending(m => m.DataMovimentacao);
    }

    public async Task<IEnumerable<Movimentacao>> GetMovimentacoesPeriodoAsync(
        string contaId,
        DateTime inicio,
        DateTime fim,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var conta = await GetByIdAsync(contaId, cancellationToken);

        if (conta == null)
            return Enumerable.Empty<Movimentacao>();

        var movimentacoes = conta.ObterMovimentacoesPeriodo(inicio, fim).ToList();

        _logger.LogInformation("Busca movimentações por período - Conta: {ContaId} - Período: {Inicio} a {Fim} - Total: {Total}",
            contaId, inicio, fim, movimentacoes.Count);

        return movimentacoes;
    }

    // Método para limpar dados (útil para testes)
    public static void LimparDados()
    {
        _contas.Clear();
        _contasPorNumero.Clear();
    }

    // Método para adicionar contas de exemplo
    public static void AdicionarContaDeExemplo(int numero, string titular, decimal saldoInicial = 0)
    {
        var conta = new Conta(numero, titular);

        if (saldoInicial > 0)
        {
            conta.Creditar(saldoInicial, "Saldo inicial de exemplo");
        }

        _contas.TryAdd(conta.Id, conta);
        _contasPorNumero.TryAdd(conta.Numero, conta);
    }

    private static void InicializarContasDeExemplo()
    {
        try
        {
            // Conta 1 - João Silva
            AdicionarContaDeExemplo(123456, "João Silva", 5000.00m);

            // Conta 2 - Maria Santos  
            AdicionarContaDeExemplo(678901, "Maria Santos", 3500.50m);

            // Conta 3 - Empresa ABC
            AdicionarContaDeExemplo(111111, "Empresa ABC Ltda", 25000.75m);

            // Conta 4 - Pedro Costa (saldo baixo para testar validações)
            AdicionarContaDeExemplo(222222, "Pedro Costa", 100.00m);

            // Conta 5 - Ana Oliveira (sem saldo inicial)
            AdicionarContaDeExemplo(333333, "Ana Oliveira", 0.00m);
        }
        catch (Exception ex)
        {
            // Log do erro, mas não impede a inicialização da aplicação
            Console.WriteLine($"Erro ao inicializar contas de exemplo: {ex.Message}");
        }
    }

    // Método para obter estatísticas do repositório
    public async Task<object> GetEstatisticasAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);

        var todasContas = _contas.Values.ToList();

        return new
        {
            TotalContas = todasContas.Count,
            ContasAtivas = todasContas.Count(c => c.Ativa),
            ContasInativas = todasContas.Count(c => !c.Ativa),
            SaldoTotal = todasContas.Sum(c => c.Saldo),
            SaldoMedio = todasContas.Count > 0 ? todasContas.Average(c => c.Saldo) : 0,
            ContaComMaiorSaldo = todasContas.OrderByDescending(c => c.Saldo).FirstOrDefault()?.Numero,
            TotalMovimentacoes = todasContas.Sum(c => c.Movimentacoes.Count)
        };
    }
}

