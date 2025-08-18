# bks.sdk

Um framework robusto para .NET 8 que oferece uma base sólida para desenvolvimento de aplicações com processamento de transações, autenticação, observabilidade e eventos de domínio.

## 🚀 Características Principais

- 🔐 **Autenticação**: Sistema completo de validação via licença e JWT
- 📊 **Observabilidade**: Integração nativa com OpenTelemetry e Serilog
- 🔄 **Transações**: Processamento seguro com tokenização
- 📡 **Eventos**: Suporte para múltiplos brokers (RabbitMQ, Kafka, Google PubSub)
- 💾 **Cache**: Cache distribuído com Redis
- 🧩 **Mediator**: Implementação própria do Mediator Pattern

## 📋 Sumário

- [Instalação](#instalação)
- [Estrutura do SDK](#estrutura-do-sdk)
- [Namespaces e Funcionalidades](#namespaces-e-funcionalidades)
- [Implementação em Minimal API](#implementação-em-minimal-api)
- [Configuração](#configuração)
- [Exemplos de Transações](#exemplos-de-transações)
- [Endpoints da API](#endpoints-da-api)
- [Arquitetura](#arquitetura)

## 📦 Instalação

```bash
dotnet add package bks.sdk
```

## 🏗️ Estrutura do SDK

```
bks.sdk/
├── Core/                # Configuração e inicialização
├── Authentication/      # Validação de licença e JWT
├── Transactions/        # Processamento de transações
├── Events/              # Eventos de domínio e brokers
├── Mediator/            # Mediator Pattern próprio
├── Observability/       # Logging e tracing
└── Cache/               # Provedor de cache Redis
```

## 🔧 Namespaces e Funcionalidades

### Core
**Configuração central do SDK**
- Inicialização e registro de dependências
- Configuração via Redis ou arquivo
- Gerenciamento do ciclo de vida da aplicação

### Authentication
**Sistema de autenticação robusto**
- Validação de licença do SDK
- Geração e validação de JWT tokens
- Middleware de autorização integrado

### Transactions
**Processamento de transações seguras**
- Base para transações tokenizáveis
- Pipeline extensível (pré/pós-processamento)
- Integração automática com eventos e logging

### Events
**Sistema de eventos distribuídos**
- Suporte para RabbitMQ, Kafka e Google PubSub
- Eventos de domínio padronizados
- Publicação e subscrição assíncrona

### Observability
**Monitoramento e diagnósticos**
- Logging estruturado com Serilog
- Tracing distribuído com OpenTelemetry
- Métricas customizadas

### Mediator
**Orquestração de casos de uso**
- Implementação própria do Mediator Pattern
- Separação clara entre apresentação e lógica de negócio
- Pipeline de validação e tratamento de erros

### Cache
**Cache distribuído**
- Provedor Redis integrado
- Configurações e dados temporários
- Suporte a TTL e invalidação

## 🏛️ Implementação em Minimal API

### Estrutura Recomendada

```
MinimalAPI/
├── Domain/
│   ├── Entities/             # Entidades de domínio
│   ├── ValueObjects/         # Objetos de valor
│   └── Transactions/         # Transações específicas (herdam BaseTransaction)
│       ├── CreditoTransaction.cs
│       ├── PagamentoCodigoBarraTransaction.cs
│       └── TransferenciaTransaction.cs
├── Application/
│   ├── UseCases/            # Handlers de casos de uso
│   ├── DTOs/                # Objetos de transferência de dados
│   └── Validators/          # Validadores de entrada
├── Infrastructure/
│   ├── EventHandlers/       # Handlers de eventos de domínio
│   ├── Repositories/        # Implementações de repositório
│   ├── Services/           # Serviços de infraestrutura
│   └── Processors/         # Processadores de transação
│       ├── CreditoProcessor.cs
│       ├── PagamentoCodigoBarraProcessor.cs
│       └── TransferenciaProcessor.cs
└── API/
    ├── Endpoints/           # Endpoints HTTP organizados
    │   ├── CreditoEndpoints.cs
    │   ├── PagamentoEndpoints.cs
    │   └── TransferenciaEndpoints.cs
    ├── Middlewares/         # Middlewares customizados
    └── Program.cs           # Configuração e DI
```

## ⚙️ Configuração

### Arquivo sdksettings.json

```json
{
  "LicenseKey": "sua-chave-de-licenca",
  "ApplicationName": "TransacoesAPI",
  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "transacoes-api",
    "Database": 0
  },
  "Jwt": {
    "SecretKey": "sua-chave-secreta-jwt-muito-segura",
    "Issuer": "TransacoesAPI",
    "Audience": "usuarios-api",
    "ExpirationInMinutes": 60
  },
  "EventBroker": {
    "BrokerType": "RabbitMQ",
    "ConnectionString": "amqp://guest:guest@localhost:5672/",
    "ExchangeName": "transacoes-events"
  },
  "Observability": {
    "ServiceName": "TransacoesAPI",
    "ServiceVersion": "1.0.0",
    "JaegerEndpoint": "http://localhost:14268/api/traces"
  }
}
```

### Configuração no Program.cs

```csharp
using bks.sdk.Core.Initialization;
using bks.sdk.Transactions;

var builder = WebApplication.CreateBuilder(args);

// Configuração do SDK
builder.Services.AddBKSSDK();

// Registro dos processadores de transação
builder.Services.AddScoped<ITransactionProcessor, CreditoProcessor>();
builder.Services.AddScoped<ITransactionProcessor, PagamentoCodigoBarraProcessor>();
builder.Services.AddScoped<ITransactionProcessor, TransferenciaProcessor>();

// Configuração da API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Transações API", 
        Version = "v1",
        Description = "API para processamento de transações financeiras"
    });
});

var app = builder.Build();

// Pipeline de middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Transações API v1");
    });
}

// Middleware do SDK (autenticação, observabilidade, etc.)
app.UseBKSSDK();

// Mapeamento dos endpoints
app.MapCreditoEndpoints();
app.MapPagamentoEndpoints();
app.MapTransferenciaEndpoints();

app.Run();
```

## 💡 Exemplos de Transações

### 1. Transação de Crédito

```csharp
// Domain/Transactions/CreditoTransaction.cs
public record CreditoTransaction : BaseTransaction
{
    public string NumeroContaCredito { get; init; } = string.Empty;
    public decimal Valor { get; init; }
    public string Descricao { get; init; } = string.Empty;
    public string? Referencia { get; init; }
}

// Infrastructure/Processors/CreditoProcessor.cs
public class CreditoProcessor : TransactionProcessor
{
    private readonly IContaRepository _contaRepository;
    private readonly ILogger<CreditoProcessor> _logger;

    public CreditoProcessor(
        IContaRepository contaRepository,
        ILogger<CreditoProcessor> logger)
    {
        _contaRepository = contaRepository;
        _logger = logger;
    }

    protected override async Task<Result> ProcessAsync(
        BaseTransaction transaction, 
        CancellationToken cancellationToken)
    {
        if (transaction is not CreditoTransaction credito)
            return Result.Failure("Tipo de transação inválido para crédito");

        // Validações de negócio
        if (credito.Valor <= 0)
            return Result.Failure("Valor deve ser maior que zero");

        if (string.IsNullOrWhiteSpace(credito.NumeroContaCredito))
            return Result.Failure("Número da conta é obrigatório");

        try
        {
            // Buscar conta destino
            var conta = await _contaRepository.GetByNumeroAsync(
                credito.NumeroContaCredito, cancellationToken);

            if (conta == null)
                return Result.Failure("Conta não encontrada");

            // Executar crédito
            conta.Creditar(credito.Valor, credito.Descricao);

            // Persistir alterações
            await _contaRepository.UpdateAsync(conta, cancellationToken);

            _logger.LogInformation("Crédito realizado com sucesso: {Valor} na conta {Conta}",
                credito.Valor, credito.NumeroContaCredito);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar crédito");
            return Result.Failure("Erro interno ao processar crédito");
        }
    }
}
```

### 2. Transação de Pagamento com Código de Barras

```csharp
// Domain/Transactions/PagamentoCodigoBarraTransaction.cs
public record PagamentoCodigoBarraTransaction : BaseTransaction
{
    public string NumeroContaDebito { get; init; } = string.Empty;
    public string CodigoBarra { get; init; } = string.Empty;
    public decimal Valor { get; init; }
    public DateTime? DataVencimento { get; init; }
    public string? NomeBeneficiario { get; init; }
}

// Infrastructure/Processors/PagamentoCodigoBarraProcessor.cs
public class PagamentoCodigoBarraProcessor : TransactionProcessor
{
    private readonly IContaRepository _contaRepository;
    private readonly ICodigoBarraService _codigoBarraService;
    private readonly ILogger<PagamentoCodigoBarraProcessor> _logger;

    public PagamentoCodigoBarraProcessor(
        IContaRepository contaRepository,
        ICodigoBarraService codigoBarraService,
        ILogger<PagamentoCodigoBarraProcessor> logger)
    {
        _contaRepository = contaRepository;
        _codigoBarraService = codigoBarraService;
        _logger = logger;
    }

    protected override async Task<Result> ProcessAsync(
        BaseTransaction transaction, 
        CancellationToken cancellationToken)
    {
        if (transaction is not PagamentoCodigoBarraTransaction pagamento)
            return Result.Failure("Tipo de transação inválido para pagamento");

        // Validações de negócio
        if (pagamento.Valor <= 0)
            return Result.Failure("Valor deve ser maior que zero");

        if (string.IsNullOrWhiteSpace(pagamento.CodigoBarra))
            return Result.Failure("Código de barras é obrigatório");

        if (string.IsNullOrWhiteSpace(pagamento.NumeroContaDebito))
            return Result.Failure("Número da conta de débito é obrigatório");

        try
        {
            // Validar código de barras
            var codigoBarraInfo = await _codigoBarraService.ValidarAsync(
                pagamento.CodigoBarra, cancellationToken);

            if (!codigoBarraInfo.IsValid)
                return Result.Failure("Código de barras inválido");

            // Verificar se o valor confere
            if (codigoBarraInfo.Valor != pagamento.Valor)
                return Result.Failure("Valor informado não confere com o código de barras");

            // Verificar vencimento
            if (codigoBarraInfo.DataVencimento < DateTime.Today)
                return Result.Failure("Boleto vencido");

            // Buscar conta de débito
            var conta = await _contaRepository.GetByNumeroAsync(
                pagamento.NumeroContaDebito, cancellationToken);

            if (conta == null)
                return Result.Failure("Conta de débito não encontrada");

            // Verificar saldo
            if (conta.Saldo < pagamento.Valor)
                return Result.Failure("Saldo insuficiente");

            // Executar pagamento
            conta.Debitar(pagamento.Valor, $"Pagamento - {codigoBarraInfo.NomeBeneficiario}");

            // Registrar pagamento no sistema do beneficiário (simulação)
            await _codigoBarraService.RegistrarPagamentoAsync(
                pagamento.CodigoBarra, pagamento.Valor, cancellationToken);

            // Persistir alterações
            await _contaRepository.UpdateAsync(conta, cancellationToken);

            _logger.LogInformation("Pagamento realizado com sucesso: {Valor} para {Beneficiario}",
                pagamento.Valor, codigoBarraInfo.NomeBeneficiario);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar pagamento");
            return Result.Failure("Erro interno ao processar pagamento");
        }
    }
}
```

### 3. Transação de Transferência

```csharp
// Domain/Transactions/TransferenciaTransaction.cs
public record TransferenciaTransaction : BaseTransaction
{
    public string NumeroContaOrigem { get; init; } = string.Empty;
    public string NumeroContaDestino { get; init; } = string.Empty;
    public decimal Valor { get; init; }
    public string Descricao { get; init; } = string.Empty;
    public TipoTransferencia Tipo { get; init; } = TipoTransferencia.TED;
}

public enum TipoTransferencia
{
    DOC = 1,
    TED = 2,
    PIX = 3
}

// Infrastructure/Processors/TransferenciaProcessor.cs
public class TransferenciaProcessor : TransactionProcessor
{
    private readonly IContaRepository _contaRepository;
    private readonly ITaxaService _taxaService;
    private readonly ILogger<TransferenciaProcessor> _logger;

    public TransferenciaProcessor(
        IContaRepository contaRepository,
        ITaxaService taxaService,
        ILogger<TransferenciaProcessor> logger)
    {
        _contaRepository = contaRepository;
        _taxaService = taxaService;
        _logger = logger;
    }

    protected override async Task<Result> ProcessAsync(
        BaseTransaction transaction, 
        CancellationToken cancellationToken)
    {
        if (transaction is not TransferenciaTransaction transferencia)
            return Result.Failure("Tipo de transação inválido para transferência");

        // Validações de negócio
        if (transferencia.Valor <= 0)
            return Result.Failure("Valor deve ser maior que zero");

        if (transferencia.NumeroContaOrigem == transferencia.NumeroContaDestino)
            return Result.Failure("Conta origem e destino devem ser diferentes");

        if (string.IsNullOrWhiteSpace(transferencia.NumeroContaOrigem) ||
            string.IsNullOrWhiteSpace(transferencia.NumeroContaDestino))
            return Result.Failure("Números das contas são obrigatórios");

        try
        {
            // Buscar contas
            var contaOrigem = await _contaRepository.GetByNumeroAsync(
                transferencia.NumeroContaOrigem, cancellationToken);
            var contaDestino = await _contaRepository.GetByNumeroAsync(
                transferencia.NumeroContaDestino, cancellationToken);

            if (contaOrigem == null)
                return Result.Failure("Conta de origem não encontrada");
            
            if (contaDestino == null)
                return Result.Failure("Conta de destino não encontrada");

            // Calcular taxa baseada no tipo de transferência
            var taxa = await _taxaService.CalcularTaxaAsync(
                transferencia.Tipo, transferencia.Valor, cancellationToken);

            var valorTotalDebito = transferencia.Valor + taxa;

            // Verificar saldo
            if (contaOrigem.Saldo < valorTotalDebito)
                return Result.Failure("Saldo insuficiente (incluindo taxa)");

            // Executar transferência
            contaOrigem.Debitar(valorTotalDebito, 
                $"Transferência {transferencia.Tipo} - {transferencia.Descricao}");
            
            contaDestino.Creditar(transferencia.Valor, 
                $"Transferência recebida - {transferencia.Descricao}");

            // Registrar taxa se houver
            if (taxa > 0)
            {
                contaOrigem.Debitar(taxa, $"Taxa {transferencia.Tipo}");
            }

            // Persistir alterações
            await _contaRepository.UpdateAsync(contaOrigem, cancellationToken);
            await _contaRepository.UpdateAsync(contaDestino, cancellationToken);

            _logger.LogInformation(
                "Transferência {Tipo} realizada: {Valor} de {Origem} para {Destino} (Taxa: {Taxa})",
                transferencia.Tipo, transferencia.Valor, 
                transferencia.NumeroContaOrigem, transferencia.NumeroContaDestino, taxa);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar transferência");
            return Result.Failure("Erro interno ao processar transferência");
        }
    }
}
```

## 🌐 Endpoints da API

### 1. Endpoint de Crédito

```csharp
// API/Endpoints/CreditoEndpoints.cs
public static class CreditoEndpoints
{
    public static void MapCreditoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/credito")
            .WithTags("Crédito")
            .WithOpenApi();

        group.MapPost("/", async (
            CreditoRequest request,
            ITransactionProcessor processor,
            CancellationToken cancellationToken) =>
        {
            var transacao = new CreditoTransaction
            {
                NumeroContaCredito = request.NumeroContaCredito,
                Valor = request.Valor,
                Descricao = request.Descricao,
                Referencia = request.Referencia
            };

            var resultado = await processor.ExecuteAsync(transacao, cancellationToken);

            return resultado.IsSuccess 
                ? Results.Ok(new CreditoResponse
                {
                    Sucesso = true,
                    Mensagem = "Crédito realizado com sucesso!",
                    TransacaoId = transacao.Id,
                    ValorCreditado = request.Valor
                })
                : Results.BadRequest(new CreditoResponse
                {
                    Sucesso = false,
                    Mensagem = resultado.Error,
                    TransacaoId = transacao.Id
                });
        })
        .WithName("RealizarCredito")
        .WithSummary("Realizar crédito em conta")
        .WithDescription("Credita um valor em uma conta específica")
        .Produces<CreditoResponse>(200)
        .Produces<CreditoResponse>(400);
    }
}

public record CreditoRequest(
    string NumeroContaCredito,
    decimal Valor,
    string Descricao,
    string? Referencia = null);

public record CreditoResponse
{
    public bool Sucesso { get; init; }
    public string Mensagem { get; init; } = string.Empty;
    public string? TransacaoId { get; init; }
    public decimal? ValorCreditado { get; init; }
}
```

### 2. Endpoint de Pagamento com Código de Barras

```csharp
// API/Endpoints/PagamentoEndpoints.cs
public static class PagamentoEndpoints
{
    public static void MapPagamentoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/pagamentos")
            .WithTags("Pagamentos")
            .WithOpenApi();

        group.MapPost("/codigo-barra", async (
            PagamentoCodigoBarraRequest request,
            ITransactionProcessor processor,
            CancellationToken cancellationToken) =>
        {
            var transacao = new PagamentoCodigoBarraTransaction
            {
                NumeroContaDebito = request.NumeroContaDebito,
                CodigoBarra = request.CodigoBarra,
                Valor = request.Valor,
                DataVencimento = request.DataVencimento,
                NomeBeneficiario = request.NomeBeneficiario
            };

            var resultado = await processor.ExecuteAsync(transacao, cancellationToken);

            return resultado.IsSuccess 
                ? Results.Ok(new PagamentoResponse
                {
                    Sucesso = true,
                    Mensagem = "Pagamento realizado com sucesso!",
                    TransacaoId = transacao.Id,
                    ValorPago = request.Valor,
                    Beneficiario = request.NomeBeneficiario
                })
                : Results.BadRequest(new PagamentoResponse
                {
                    Sucesso = false,
                    Mensagem = resultado.Error,
                    TransacaoId = transacao.Id
                });
        })
        .WithName("PagarCodigoBarra")
        .WithSummary("Realizar pagamento com código de barras")
        .WithDescription("Processa pagamento de boleto através do código de barras")
        .Produces<PagamentoResponse>(200)
        .Produces<PagamentoResponse>(400);

        group.MapPost("/validar-codigo-barra", async (
            ValidarCodigoBarraRequest request,
            ICodigoBarraService codigoBarraService,
            CancellationToken cancellationToken) =>
        {
            var validacao = await codigoBarraService.ValidarAsync(
                request.CodigoBarra, cancellationToken);

            return validacao.IsValid 
                ? Results.Ok(new ValidarCodigoBarraResponse
                {
                    Valido = true,
                    Valor = validacao.Valor,
                    DataVencimento = validacao.DataVencimento,
                    NomeBeneficiario = validacao.NomeBeneficiario
                })
                : Results.BadRequest(new ValidarCodigoBarraResponse
                {
                    Valido = false,
                    Erro = "Código de barras inválido"
                });
        })
        .WithName("ValidarCodigoBarra")
        .WithSummary("Validar código de barras")
        .WithDescription("Valida um código de barras e retorna as informações do boleto");
    }
}

public record PagamentoCodigoBarraRequest(
    string NumeroContaDebito,
    string CodigoBarra,
    decimal Valor,
    DateTime? DataVencimento = null,
    string? NomeBeneficiario = null);

public record PagamentoResponse
{
    public bool Sucesso { get; init; }
    public string Mensagem { get; init; } = string.Empty;
    public string? TransacaoId { get; init; }
    public decimal? ValorPago { get; init; }
    public string? Beneficiario { get; init; }
}

public record ValidarCodigoBarraRequest(string CodigoBarra);

public record ValidarCodigoBarraResponse
{
    public bool Valido { get; init; }
    public decimal? Valor { get; init; }
    public DateTime? DataVencimento { get; init; }
    public string? NomeBeneficiario { get; init; }
    public string? Erro { get; init; }
}
```

### 3. Endpoint de Transferência

```csharp
// API/Endpoints/TransferenciaEndpoints.cs
public static class TransferenciaEndpoints
{
    public static void MapTransferenciaEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/transferencias")
            .WithTags("Transferências")
            .WithOpenApi();

        group.MapPost("/", async (
            TransferenciaRequest request,
            ITransactionProcessor processor,
            CancellationToken cancellationToken) =>
        {
            var transacao = new TransferenciaTransaction
            {
                NumeroContaOrigem = request.NumeroContaOrigem,
                NumeroContaDestino = request.NumeroContaDestino,
                Valor = request.Valor,
                Descricao = request.Descricao,
                Tipo = request.Tipo
            };

            var resultado = await processor.ExecuteAsync(transacao, cancellationToken);

            return resultado.IsSuccess 
                ? Results.Ok(new TransferenciaResponse
                {
                    Sucesso = true,
                    Mensagem = "Transferência realizada com sucesso!",
                    TransacaoId = transacao.Id,
                    ValorTransferido = request.Valor,
                    TipoTransferencia = request.Tipo.ToString()
                })
                : Results.BadRequest(new TransferenciaResponse
                {
                    Sucesso = false,
                    Mensagem = resultado.Error,
                    TransacaoId = transacao.Id
                });
        })
        .WithName("RealizarTransferencia")
        .WithSummary("Realizar transferência entre contas")
        .WithDescription("Transfere valores entre contas com diferentes tipos (DOC, TED, PIX)")
        .Produces<TransferenciaResponse>(200)
        .Produces<TransferenciaResponse>(400);

        group.MapGet("/taxas/{tipo}", async (
            TipoTransferencia tipo,
            [FromQuery] decimal valor,
            ITaxaService taxaService,
            CancellationToken cancellationToken) =>
        {
            var taxa = await taxaService.CalcularTaxaAsync(tipo, valor, cancellationToken);

            return Results.Ok(new TaxaResponse
            {
                TipoTransferencia = tipo.ToString(),
                ValorTransferencia = valor,
                Taxa = taxa,
                ValorTotal = valor + taxa
            });
        })
        .WithName("ConsultarTaxa")
        .WithSummary("Consultar taxa de transferência")
        .WithDescription("Consulta a taxa aplicável para um tipo de transferência")
        .Produces<TaxaResponse>(200);
    }
}

public record TransferenciaRequest(
    string NumeroContaOrigem,
    string NumeroContaDestino,
    decimal Valor,
    string Descricao,
    TipoTransferencia Tipo = TipoTransferencia.TED);

public record TransferenciaResponse
{
    public bool Sucesso { get; init; }
    public string Mensagem { get; init; } = string.Empty;
    public string? TransacaoId { get; init; }
    public decimal? ValorTransferido { get; init; }
    public string? TipoTransferencia { get; init; }
}

public record TaxaResponse
{
    public string TipoTransferencia { get; init; } = string.Empty;
    public decimal ValorTransferencia { get; init; }
    public decimal Taxa { get; init; }
    public decimal ValorTotal { get; init; }
}
```

## 🏗️ Arquitetura

O bks.sdk segue os princípios da Clean Architecture, proporcionando:

- **Separação de responsabilidades** clara entre camadas
- **Inversão de dependência** através de abstrações bem definidas
- **Testabilidade** com interfaces mockáveis
- **Extensibilidade** através de pontos de extensão bem definidos
- **Observabilidade** integrada em todos os níveis

### Fluxo de Execução

1. **Request** chega pelo endpoint HTTP específico
2. **Middleware** do SDK processa autenticação e observabilidade
3. **Endpoint Handler** cria a transação apropriada
4. **Transaction Processor** específico executa a lógica de negócio
5. **Events** são publicados automaticamente pelo SDK
6. **Response** é retornado com logging e tracing completos

### Tipos de Transação Suportados

| Tipo | Descrição | Processador | Endpoint |
|------|-----------|------------|----------|
| **Crédito** | Credita valor em conta | `CreditoProcessor` | `POST /api/credito` |
| **Pagamento** | Paga boleto via código de barras | `PagamentoCodigoBarraProcessor` | `POST /api/pagamentos/codigo-barra` |
| **Transferência** | Transfere entre contas (DOC/TED/PIX) | `TransferenciaProcessor` | `POST /api/transferencias` |

## 📚 Testes da API

### Exemplos de Requisições

#### Crédito
```bash
curl -X POST "https://localhost:7001/api/credito" \
  -H "Content-Type: application/json" \
  -d '{
    "numeroContaCredito": "12345-6",
    "valor": 1000.00,
    "descricao": "Crédito de salário",
    "referencia": "SAL-2025-001"
  }'
```

#### Pagamento com Código de Barras
```bash
curl -X POST "https://localhost:7001/api/pagamentos/codigo-barra" \
  -H "Content-Type: application/json" \
  -d '{
    "numeroContaDebito": "12345-6",
    "codigoBarra": "34191790010104351004791020150008291070000002000",
    "valor": 200.00,
    "nomeBeneficiario": "Empresa XYZ Ltda"
  }'
```

#### Transferência
```bash
curl -X POST "https://localhost:7001/api/transferencias" \
  -H "Content-Type: application/json" \
  -d '{
    "numeroContaOrigem": "12345-6",
    "numeroContaDestino": "67890-1",
    "valor": 500.00,
    "descricao": "Transferência para fornecedor",
    "tipo": 2
  }'
```

## 🔧 Interfaces e Serviços Necessários

Para implementar a API completa, você precisará criar as seguintes interfaces e implementações:

### Repositórios

```csharp
// Infrastructure/Repositories/IContaRepository.cs
public interface IContaRepository
{
    Task<Conta?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<Conta?> GetByNumeroAsync(string numero, CancellationToken cancellationToken);
    Task UpdateAsync(Conta conta, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string numero, CancellationToken cancellationToken);
}
```

### Serviços de Domínio

```csharp
// Infrastructure/Services/ICodigoBarraService.cs
public interface ICodigoBarraService
{
    Task<CodigoBarraInfo> ValidarAsync(string codigoBarra, CancellationToken cancellationToken);
    Task RegistrarPagamentoAsync(string codigoBarra, decimal valor, CancellationToken cancellationToken);
}

public record CodigoBarraInfo
{
    public bool IsValid { get; init; }
    public decimal Valor { get; init; }
    public DateTime DataVencimento { get; init; }
    public string NomeBeneficiario { get; init; } = string.Empty;
    public string TipoBoleto { get; init; } = string.Empty;
}

// Infrastructure/Services/ITaxaService.cs
public interface ITaxaService
{
    Task<decimal> CalcularTaxaAsync(TipoTransferencia tipo, decimal valor, CancellationToken cancellationToken);
}
```

### Entidade de Domínio

```csharp
// Domain/Entities/Conta.cs
public class Conta
{
    public string Id { get; private set; } = string.Empty;
    public string Numero { get; private set; } = string.Empty;
    public string Titular { get; private set; } = string.Empty;
    public decimal Saldo { get; private set; }
    public bool Ativa { get; private set; }
    public DateTime DataCriacao { get; private set; }
    public DateTime? DataUltimaMovimentacao { get; private set; }

    private readonly List<Movimentacao> _movimentacoes = new();
    public IReadOnlyList<Movimentacao> Movimentacoes => _movimentacoes.AsReadOnly();

    public Conta(string numero, string titular)
    {
        Id = Guid.NewGuid().ToString();
        Numero = numero;
        Titular = titular;
        Saldo = 0;
        Ativa = true;
        DataCriacao = DateTime.UtcNow;
    }

    public void Creditar(decimal valor, string descricao = "")
    {
        if (valor <= 0)
            throw new ArgumentException("Valor deve ser maior que zero", nameof(valor));

        if (!Ativa)
            throw new InvalidOperationException("Conta inativa");

        Saldo += valor;
        DataUltimaMovimentacao = DateTime.UtcNow;

        _movimentacoes.Add(new Movimentacao
        {
            Id = Guid.NewGuid().ToString(),
            ContaId = Id,
            Tipo = TipoMovimentacao.Credito,
            Valor = valor,
            Descricao = descricao,
            DataMovimentacao = DataUltimaMovimentacao.Value,
            SaldoAnterior = Saldo - valor,
            SaldoPosterior = Saldo
        });
    }

    public void Debitar(decimal valor, string descricao = "")
    {
        if (valor <= 0)
            throw new ArgumentException("Valor deve ser maior que zero", nameof(valor));

        if (!Ativa)
            throw new InvalidOperationException("Conta inativa");

        if (Saldo < valor)
            throw new InvalidOperationException("Saldo insuficiente");

        Saldo -= valor;
        DataUltimaMovimentacao = DateTime.UtcNow;

        _movimentacoes.Add(new Movimentacao
        {
            Id = Guid.NewGuid().ToString(),
            ContaId = Id,
            Tipo = TipoMovimentacao.Debito,
            Valor = valor,
            Descricao = descricao,
            DataMovimentacao = DataUltimaMovimentacao.Value,
            SaldoAnterior = Saldo + valor,
            SaldoPosterior = Saldo
        });
    }

    public void Inativar()
    {
        Ativa = false;
    }

    public void Ativar()
    {
        Ativa = true;
    }
}

public class Movimentacao
{
    public string Id { get; init; } = string.Empty;
    public string ContaId { get; init; } = string.Empty;
    public TipoMovimentacao Tipo { get; init; }
    public decimal Valor { get; init; }
    public string Descricao { get; init; } = string.Empty;
    public DateTime DataMovimentacao { get; init; }
    public decimal SaldoAnterior { get; init; }
    public decimal SaldoPosterior { get; init; }
}

public enum TipoMovimentacao
{
    Debito = 1,
    Credito = 2
}
```

## 🚀 Implementações de Exemplo

### Implementação Simples do CodigoBarraService

```csharp
// Infrastructure/Services/CodigoBarraService.cs
public class CodigoBarraService : ICodigoBarraService
{
    private readonly ILogger<CodigoBarraService> _logger;

    public CodigoBarraService(ILogger<CodigoBarraService> logger)
    {
        _logger = logger;
    }

    public async Task<CodigoBarraInfo> ValidarAsync(string codigoBarra, CancellationToken cancellationToken)
    {
        // Simulação de validação de código de barras
        // Em um cenário real, consultaria APIs de bancos ou sistemas de cobrança
        
        if (string.IsNullOrWhiteSpace(codigoBarra) || codigoBarra.Length != 47)
        {
            return new CodigoBarraInfo { IsValid = false };
        }

        // Extrair informações do código de barras (simulação)
        var valor = ExtrairValor(codigoBarra);
        var dataVencimento = ExtrairDataVencimento(codigoBarra);
        var beneficiario = "Empresa Exemplo Ltda";

        await Task.Delay(100, cancellationToken); // Simula chamada externa

        return new CodigoBarraInfo
        {
            IsValid = true,
            Valor = valor,
            DataVencimento = dataVencimento,
            NomeBeneficiario = beneficiario,
            TipoBoleto = "Cobrança Registrada"
        };
    }

    public async Task RegistrarPagamentoAsync(string codigoBarra, decimal valor, CancellationToken cancellationToken)
    {
        // Simulação de registro de pagamento no sistema do beneficiário
        _logger.LogInformation("Registrando pagamento: Código {Codigo}, Valor {Valor}", 
            codigoBarra, valor);
        
        await Task.Delay(200, cancellationToken); // Simula chamada externa
    }

    private decimal ExtrairValor(string codigoBarra)
    {
        // Simulação de extração do valor do código de barras
        // Posições 37-47 em códigos de barras bancários
        var valorStr = codigoBarra.Substring(37, 10);
        return decimal.Parse(valorStr) / 100; // Valor em centavos
    }

    private DateTime ExtrairDataVencimento(string codigoBarra)
    {
        // Simulação de extração da data de vencimento
        // Em códigos reais, seria extraído das posições específicas
        return DateTime.Today.AddDays(30);
    }
}
```

### Implementação Simples do TaxaService

```csharp
// Infrastructure/Services/TaxaService.cs
public class TaxaService : ITaxaService
{
    private readonly ILogger<TaxaService> _logger;

    public TaxaService(ILogger<TaxaService> logger)
    {
        _logger = logger;
    }

    public async Task<decimal> CalcularTaxaAsync(TipoTransferencia tipo, decimal valor, CancellationToken cancellationToken)
    {
        await Task.Delay(50, cancellationToken); // Simula consulta de tabela de taxas

        var taxa = tipo switch
        {
            TipoTransferencia.PIX => 0m, // PIX é gratuito
            TipoTransferencia.TED => valor switch
            {
                <= 5000 => 15.90m,
                <= 10000 => 25.90m,
                _ => 35.90m
            },
            TipoTransferencia.DOC => valor switch
            {
                <= 5000 => 12.90m,
                <= 10000 => 20.90m,
                _ => 30.90m
            },
            _ => 0m
        };

        _logger.LogInformation("Taxa calculada para {Tipo}: R$ {Taxa} (Valor: R$ {Valor})", 
            tipo, taxa, valor);

        return taxa;
    }
}
```

### Implementação Simples do ContaRepository (In-Memory)

```csharp
// Infrastructure/Repositories/ContaRepository.cs
public class ContaRepository : IContaRepository
{
    private readonly ILogger<ContaRepository> _logger;
    private static readonly ConcurrentDictionary<string, Conta> _contas = new();

    static ContaRepository()
    {
        // Dados de exemplo para teste
        var conta1 = new Conta("12345-6", "João Silva");
        conta1.Creditar(5000, "Saldo inicial");
        
        var conta2 = new Conta("67890-1", "Maria Santos");
        conta2.Creditar(3000, "Saldo inicial");

        _contas.TryAdd(conta1.Id, conta1);
        _contas.TryAdd(conta2.Id, conta2);
    }

    public ContaRepository(ILogger<ContaRepository> logger)
    {
        _logger = logger;
    }

    public async Task<Conta?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
        return _contas.TryGetValue(id, out var conta) ? conta : null;
    }

    public async Task<Conta?> GetByNumeroAsync(string numero, CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
        return _contas.Values.FirstOrDefault(c => c.Numero == numero);
    }

    public async Task UpdateAsync(Conta conta, CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
        _contas.AddOrUpdate(conta.Id, conta, (key, oldValue) => conta);
        
        _logger.LogInformation("Conta atualizada: {Numero} - Saldo: R$ {Saldo}", 
            conta.Numero, conta.Saldo);
    }

    public async Task<bool> ExistsAsync(string numero, CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
        return _contas.Values.Any(c => c.Numero == numero);
    }
}
```

## 📝 Configuração Completa do Program.cs

```csharp
using bks.sdk.Core.Initialization;
using bks.sdk.Transactions;

var builder = WebApplication.CreateBuilder(args);

// Configuração do SDK
builder.Services.AddBKSSDK();

// Registro dos processadores de transação
builder.Services.AddScoped<ITransactionProcessor, CreditoProcessor>();
builder.Services.AddScoped<ITransactionProcessor, PagamentoCodigoBarraProcessor>();
builder.Services.AddScoped<ITransactionProcessor, TransferenciaProcessor>();

// Registro dos repositórios e serviços
builder.Services.AddScoped<IContaRepository, ContaRepository>();
builder.Services.AddScoped<ICodigoBarraService, CodigoBarraService>();
builder.Services.AddScoped<ITaxaService, TaxaService>();

// Configuração da API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Transações API", 
        Version = "v1",
        Description = "API para processamento de transações financeiras usando bks.sdk",
        Contact = new() 
        { 
            Name = "Equipe BKS", 
            Email = "contato@bks.com" 
        }
    });
    
    // Adicionar exemplos para o Swagger
    c.UseAllOfToExtendReferenceSchemas();
    c.EnableAnnotations();
});

// Configuração de CORS (se necessário)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Pipeline de middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Transações API v1");
        c.RoutePrefix = string.Empty; // Swagger na raiz
    });
}

// Middleware do SDK (autenticação, observabilidade, etc.)
app.UseBKSSDK();

// CORS (se configurado)
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAll");
}

// Middleware de tratamento de erros
app.UseExceptionHandler("/error");

// Endpoint de health check
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
   .WithTags("Health")
   .WithSummary("Verificar saúde da API");

// Mapeamento dos endpoints de transações
app.MapCreditoEndpoints();
app.MapPagamentoEndpoints();
app.MapTransferenciaEndpoints();

// Endpoint de informações da API
app.MapGet("/", () => Results.Redirect("/swagger"))
   .ExcludeFromDescription();

app.Run();
```

## 🧪 Testes de Integração

### Exemplo de Teste com xUnit

```csharp
// Tests/Integration/TransacoesApiTests.cs
public class TransacoesApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TransacoesApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task RealizarCredito_DeveRetornarSucesso()
    {
        // Arrange
        var request = new CreditoRequest(
            NumeroContaCredito: "12345-6",
            Valor: 1000.00m,
            Descricao: "Teste de crédito",
            Referencia: "TEST-001"
        );

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/credito", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CreditoResponse>(responseContent);
        
        result.Should().NotBeNull();
        result.Sucesso.Should().BeTrue();
        result.ValorCreditado.Should().Be(1000.00m);
    }

    [Fact]
    public async Task RealizarTransferencia_ComSaldoInsuficiente_DeveRetornarErro()
    {
        // Arrange
        var request = new TransferenciaRequest(
            NumeroContaOrigem: "12345-6",
            NumeroContaDestino: "67890-1",
            Valor: 10000.00m, // Valor maior que o saldo
            Descricao: "Teste transferência",
            Tipo: TipoTransferencia.TED
        );

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/transferencias", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

## 📊 Monitoramento e Observabilidade

O SDK já inclui observabilidade integrada, mas você pode adicionar métricas customizadas:

```csharp
// Infrastructure/Metrics/TransacaoMetrics.cs
public class TransacaoMetrics
{
    private readonly IMetrics _metrics;

    public TransacaoMetrics(IMetrics metrics)
    {
        _metrics = metrics;
    }

    public void IncrementarTransacaoProcessada(string tipo)
    {
        _metrics.Measure.Counter.Increment(
            MetricsRegistry.Counters.TransacoesProcessadas,
            new MetricTags("tipo", tipo));
    }

    public void RegistrarTempoProcessamento(string tipo, TimeSpan duracao)
    {
        _metrics.Measure.Timer.Time(
            MetricsRegistry.Timers.TempoProcessamentoTransacao,
            duracao,
            new MetricTags("tipo", tipo));
    }
}
```

## 📚 Próximos Passos

1. **Implementar persistência real** - Substitua o repositório in-memory por Entity Framework ou Dapper
2. **Adicionar validações** - Implemente FluentValidation para as requests
3. **Configurar autenticação** - Configure JWT Bearer authentication
4. **Implementar testes unitários** - Crie testes para os processadores e serviços
5. **Configurar CI/CD** - Configure pipeline de build e deploy
6. **Documentação avançada** - Adicione OpenAPI annotations detalhadas
7. **Implementar rate limiting** - Configure limitação de requisições
8. **Adicionar cache** - Implemente cache para consultas frequentes

## 🤝 Suporte

Para dúvidas, sugestões ou problemas:

- 📧 Abra uma [issue](../../issues) no repositório
- 💬 Entre em contato com o time responsável
- 📖 Consulte a [documentação completa](docs/)
- 🧪 Veja os [exemplos de teste](examples/tests/)

---

**bks.sdk** - Desenvolvido com ❤️ para acelerar o desenvolvimento de aplicações .NET robustas e escaláveis.