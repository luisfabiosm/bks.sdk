# BKS SDK - Framework para .NET 8

[![Version](https://img.shields.io/badge/version-1.0.2-blue.svg)](https://github.com/bks-sdk/bks-sdk)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-proprietary-red.svg)]()

Um framework robusto e modular para .NET 8 que oferece uma base sólida para desenvolvimento de aplicações financeiras com processamento de transações, autenticação, observabilidade e eventos de domínio.

## 🚀 Características Principais

- 🔐 **Autenticação Completa**: Sistema de validação de licença e JWT integrado
- 📊 **Observabilidade Nativa**: OpenTelemetry, Serilog e tracing distribuído
- 🔄 **Processamento de Transações**: Pipeline seguro com tokenização e eventos
- 📡 **Sistema de Eventos**: Suporte para RabbitMQ, Kafka e Google PubSub
- 💾 **Cache Distribuído**: Redis e In-Memory com interface unificada
- 🧩 **Mediator Pattern**: Implementação própria para CQRS
- 🏗️ **Clean Architecture**: Separação clara de responsabilidades
- 🔒 **Segurança**: Criptografia, correlação de transações e auditoria

## 📋 Índice

- [Padrões Arquiteturais](#-padrões-arquiteturais)
- [Estrutura e Namespaces](#-estrutura-e-namespaces)
- [Instalação](#-instalação)
- [Configuração](#-configuração)
- [Exemplos de Uso](#-exemplos-de-uso)
- [API de Transações](#-api-de-transações)
- [Testes e Validação](#-testes-e-validação)
- [Monitoramento](#-monitoramento)

## 🏛️ Padrões Arquiteturais

### Clean Architecture
O SDK segue os princípios da Clean Architecture com separação clara entre:
- **Core**: Regras de negócio e configurações centrais
- **Application**: Casos de uso e orquestração (Mediator)
- **Infrastructure**: Implementações técnicas (Cache, Events, Auth)
- **Presentation**: Middlewares e configurações de API

### Domain-Driven Design (DDD)
- **Eventos de Domínio**: Modelagem de eventos importantes do negócio
- **Aggregates**: Transações como agregados com comportamentos encapsulados
- **Value Objects**: Objetos imutáveis para dados de transação
- **Repository Pattern**: Abstração para persistência

### CQRS (Command Query Responsibility Segregation)
- **Commands**: Transações que modificam estado
- **Queries**: Consultas de dados somente leitura
- **Handlers**: Processadores específicos por tipo de operação
- **Mediator**: Orquestração centralizada de comandos e queries

### Event Sourcing (Parcial)
- **Eventos de Transação**: Histórico completo de mudanças de estado
- **Event Dispatcher**: Publicação assíncrona de eventos
- **Event Handlers**: Processamento reativo de eventos

### Outros Padrões
- **Pipeline Pattern**: Pré e pós-processamento de transações
- **Factory Pattern**: Criação de processadores e brokers
- **Strategy Pattern**: Diferentes implementações de cache e eventos
- **Decorator Pattern**: Middlewares para cross-cutting concerns
- **Result Pattern**: Tratamento de erros sem exceções

## 🏗️ Estrutura e Namespaces

### `bks.sdk.Core`
**Configuração e inicialização central do SDK**

- **Configuration**: Gerenciamento de configurações via JSON/Redis
- **Middlewares**: Cross-cutting concerns (logging, correlação, auth)
- **Initialization**: Bootstrap e registro de dependências

**Principais Classes:**
- `SDKSettings`: Configurações centralizadas
- `TransactionCorrelationMiddleware`: Rastreamento de requisições
- `RequestLoggingMiddleware`: Log estruturado de requests

### `bks.sdk.Authentication`
**Sistema de autenticação e autorização**

- **License Validation**: Validação de licenças do SDK
- **JWT Management**: Geração e validação de tokens JWT
- **Security**: Criptografia e segurança de dados

**Principais Interfaces:**
- `ILicenseValidator`: Validação de licenças
- `IJwtTokenProvider`: Gerenciamento de tokens JWT

### `bks.sdk.Transactions`
**Núcleo do processamento de transações financeiras**

- **Base Transaction**: Classe base para todas as transações
- **Processors**: Implementações específicas de processamento
- **Events**: Eventos de ciclo de vida de transações
- **Tokenization**: Serialização segura e recuperação de transações

**Principais Classes:**
- `BaseTransaction`: Classe abstrata para transações
- `ITransactionProcessor`: Interface para processadores
- `TransactionTokenService`: Tokenização de transações
- Eventos: `TransactionStartedEvent`, `TransactionCompletedEvent`, etc.

### `bks.sdk.Events`
**Sistema de eventos distribuídos**

- **Domain Events**: Modelagem de eventos de negócio
- **Event Brokers**: Integração com sistemas de mensageria
- **Dispatching**: Publicação e consumo de eventos

**Principais Interfaces:**
- `IDomainEvent`: Contrato para eventos de domínio
- `IEventBroker`: Abstração para brokers de mensagem
- `DomainEventDispatcher`: Dispatcher interno de eventos

### `bks.sdk.Mediator`
**Implementação do padrão Mediator para CQRS**

- **Request/Response**: Modelagem de comandos e queries
- **Handlers**: Processadores de comandos específicos
- **Pipeline**: Orquestração de casos de uso

**Principais Interfaces:**
- `IMediator`: Interface principal do mediator
- `IRequest<TResponse>`: Contrato para requests
- `IRequestHandler<TRequest, TResponse>`: Handlers de comandos

### `bks.sdk.Observability`
**Monitoramento e diagnósticos**

- **Logging**: Integração com Serilog
- **Tracing**: OpenTelemetry para tracing distribuído
- **Metrics**: Coleta de métricas customizadas

**Principais Interfaces:**
- `ILogger`: Interface de logging estruturado
- `ITracer`: Interface para tracing distribuído

### `bks.sdk.Cache`
**Cache distribuído e local**

- **Abstrações**: Interface unificada para diferentes provedores
- **Implementations**: Redis e In-Memory
- **TTL Management**: Controle de tempo de vida

**Principais Interfaces:**
- `ICacheProvider`: Interface unificada para cache

### `bks.sdk.Common`
**Utilitários e tipos comuns**

- **Results**: Padrão Result para tratamento de erros
- **Validation**: Validações e regras de negócio
- **Extensions**: Métodos de extensão utilitários

## 📦 Instalação

```bash
dotnet add package bks.sdk
```

## ⚙️ Configuração

### Arquivo `appsettings.json`

```json
{
  "bkssdk": {
    "LicenseKey": "BKS-2025-PREMIUM-KEY",
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
      "AdditionalSettings": {
        "ExchangeName": "transacoes-events",
        "QueuePrefix": "transacoes",
        "RetryAttempts": "3"
      }
    },
    "Observability": {
      "ServiceName": "TransacoesAPI",
      "ServiceVersion": "1.0.0",
      "JaegerEndpoint": "http://localhost:14268/api/traces"
    }
  }
}
```

### Configuração no `Program.cs`

```csharp
using bks.sdk.Core.Initialization;
using bks.sdk.Transactions;

var builder = WebApplication.CreateBuilder(args);

// Configuração do SDK
builder.Services.AddBKSSDK();

// Registro dos processadores de transação
builder.Services.AddScoped<ITransactionProcessor, DebitoProcessor>();
builder.Services.AddScoped<ITransactionProcessor, TransferenciaProcessor>();
builder.Services.AddScoped<ITransactionProcessor, PagamentoBoletoProcessor>();

// Registro de repositórios e serviços
builder.Services.AddScoped<IContaRepository, ContaRepository>();
builder.Services.AddScoped<IBoletoService, BoletoService>();
builder.Services.AddScoped<ITaxaService, TaxaService>();

var app = builder.Build();

// Configuração de middlewares
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware do SDK (inclui auth, observability, correlação)
app.UseBKSSDK();

// Mapeamento dos endpoints
app.MapTransacaoEndpoints();

app.Run();
```

## 💡 Exemplos de Uso

### 1. Transação de Débito

#### Definição da Transação

```csharp
// Domain/Transactions/DebitoTransaction.cs
public record DebitoTransaction : BaseTransaction
{
    public string NumeroContaDebito { get; init; } = string.Empty;
    public decimal Valor { get; init; }
    public string Descricao { get; init; } = string.Empty;
    public string? Referencia { get; init; }
}
```

#### Processador de Débito

```csharp
// Infrastructure/Processors/DebitoProcessor.cs
public class DebitoProcessor : TransactionProcessor
{
    private readonly IContaRepository _contaRepository;
    private readonly ILogger<DebitoProcessor> _logger;

    public DebitoProcessor(
        IContaRepository contaRepository,
        ILogger<DebitoProcessor> logger)
    {
        _contaRepository = contaRepository;
        _logger = logger;
    }

    protected override async Task<Result> ProcessAsync(
        BaseTransaction transaction, 
        CancellationToken cancellationToken)
    {
        if (transaction is not DebitoTransaction debito)
            return Result.Failure("Tipo de transação inválido para débito");

        // Validações de negócio
        if (debito.Valor <= 0)
            return Result.Failure("Valor deve ser maior que zero");

        if (string.IsNullOrWhiteSpace(debito.NumeroContaDebito))
            return Result.Failure("Número da conta é obrigatório");

        try
        {
            // Buscar conta
            var conta = await _contaRepository.GetByNumeroAsync(
                debito.NumeroContaDebito, cancellationToken);

            if (conta == null)
                return Result.Failure("Conta não encontrada");

            // Verificar saldo
            if (conta.Saldo < debito.Valor)
                return Result.Failure("Saldo insuficiente");

            // Executar débito
            conta.Debitar(debito.Valor, debito.Descricao);

            // Persistir alterações
            await _contaRepository.UpdateAsync(conta, cancellationToken);

            _logger.LogInformation("Débito realizado com sucesso: {Valor} da conta {Conta}",
                debito.Valor, debito.NumeroContaDebito);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar débito");
            return Result.Failure("Erro interno ao processar débito");
        }
    }
}
```

### 2. Transação de Transferência

#### Definição da Transação

```csharp
// Domain/Transactions/TransferenciaTransaction.cs
public record TransferenciaTransaction : BaseTransaction
{
    public string NumeroContaOrigemDebito { get; init; } = string.Empty;
    public string NumeroContaDestinoCredito { get; init; } = string.Empty;
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
```

#### Processador de Transferência

```csharp
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

        if (transferencia.NumeroContaOrigemDebito == transferencia.NumeroContaDestinoCredito)
            return Result.Failure("Conta origem e destino devem ser diferentes");

        try
        {
            // Buscar contas
            var contaOrigem = await _contaRepository.GetByNumeroAsync(
                transferencia.NumeroContaOrigemDebito, cancellationToken);
            var contaDestino = await _contaRepository.GetByNumeroAsync(
                transferencia.NumeroContaDestinoCredito, cancellationToken);

            if (contaOrigem == null)
                return Result.Failure("Conta de origem não encontrada");
            
            if (contaDestino == null)
                return Result.Failure("Conta de destino não encontrada");

            // Calcular taxa
            var taxa = await _taxaService.CalcularTaxaAsync(
                transferencia.Tipo, transferencia.Valor, cancellationToken);

            var valorTotalDebito = transferencia.Valor + taxa;

            // Verificar saldo
            if (contaOrigem.Saldo < valorTotalDebito)
                return Result.Failure("Saldo insuficiente (incluindo taxa)");

            // Executar transferência
            contaOrigem.Debitar(transferencia.Valor, 
                $"Transferência {transferencia.Tipo} - {transferencia.Descricao}");
            
            if (taxa > 0)
                contaOrigem.Debitar(taxa, $"Taxa {transferencia.Tipo}");
            
            contaDestino.Creditar(transferencia.Valor, 
                $"Transferência recebida - {transferencia.Descricao}");

            // Persistir alterações
            await _contaRepository.UpdateAsync(contaOrigem, cancellationToken);
            await _contaRepository.UpdateAsync(contaDestino, cancellationToken);

            _logger.LogInformation(
                "Transferência {Tipo} realizada: {Valor} de {Origem} para {Destino} (Taxa: {Taxa})",
                transferencia.Tipo, transferencia.Valor, 
                transferencia.NumeroContaOrigemDebito, transferencia.NumeroContaDestinoCredito, taxa);

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

### 3. Transação de Pagamento de Boleto

#### Definição da Transação

```csharp
// Domain/Transactions/PagamentoBoletoTransaction.cs
public record PagamentoBoletoTransaction : BaseTransaction
{
    public string NumeroContaDebito { get; init; } = string.Empty;
    public string CodigoBarras { get; init; } = string.Empty;
    public decimal Valor { get; init; }
    public DateTime? DataVencimento { get; init; }
    public string? NomeBeneficiario { get; init; }
}
```

#### Processador de Pagamento de Boleto

```csharp
// Infrastructure/Processors/PagamentoBoletoProcessor.cs
public class PagamentoBoletoProcessor : TransactionProcessor
{
    private readonly IContaRepository _contaRepository;
    private readonly IBoletoService _boletoService;
    private readonly ILogger<PagamentoBoletoProcessor> _logger;

    public PagamentoBoletoProcessor(
        IContaRepository contaRepository,
        IBoletoService boletoService,
        ILogger<PagamentoBoletoProcessor> logger)
    {
        _contaRepository = contaRepository;
        _boletoService = boletoService;
        _logger = logger;
    }

    protected override async Task<Result> ProcessAsync(
        BaseTransaction transaction, 
        CancellationToken cancellationToken)
    {
        if (transaction is not PagamentoBoletoTransaction pagamento)
            return Result.Failure("Tipo de transação inválido para pagamento");

        // Validações de negócio
        if (pagamento.Valor <= 0)
            return Result.Failure("Valor deve ser maior que zero");

        if (string.IsNullOrWhiteSpace(pagamento.CodigoBarras))
            return Result.Failure("Código de barras é obrigatório");

        try
        {
            // Validar código de barras
            var boletoInfo = await _boletoService.ValidarCodigoBarrasAsync(
                pagamento.CodigoBarras, cancellationToken);

            if (!boletoInfo.IsValid)
                return Result.Failure("Código de barras inválido");

            // Verificar valor
            if (Math.Abs(boletoInfo.Valor - pagamento.Valor) > 0.01m)
                return Result.Failure("Valor informado não confere com o boleto");

            // Verificar vencimento
            if (boletoInfo.DataVencimento < DateTime.Today)
            {
                // Aplicar multa/juros se necessário
                var valorComJuros = await _boletoService.CalcularValorComJurosAsync(
                    boletoInfo, cancellationToken);
                
                if (Math.Abs(valorComJuros - pagamento.Valor) > 0.01m)
                    return Result.Failure($"Boleto vencido. Valor com juros: {valorComJuros:C}");
            }

            // Buscar conta de débito
            var conta = await _contaRepository.GetByNumeroAsync(
                pagamento.NumeroContaDebito, cancellationToken);

            if (conta == null)
                return Result.Failure("Conta de débito não encontrada");

            // Verificar saldo
            if (conta.Saldo < pagamento.Valor)
                return Result.Failure("Saldo insuficiente");

            // Executar pagamento
            conta.Debitar(pagamento.Valor, 
                $"Pagamento boleto - {boletoInfo.NomeBeneficiario}");

            // Registrar pagamento no sistema do beneficiário
            await _boletoService.RegistrarPagamentoAsync(
                pagamento.CodigoBarras, pagamento.Valor, cancellationToken);

            // Persistir alterações
            await _contaRepository.UpdateAsync(conta, cancellationToken);

            _logger.LogInformation(
                "Pagamento de boleto realizado: {Valor} para {Beneficiario}",
                pagamento.Valor, boletoInfo.NomeBeneficiario);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar pagamento de boleto");
            return Result.Failure("Erro interno ao processar pagamento");
        }
    }
}
```

## 🌐 API de Transações

### Endpoints de Transações

```csharp
// API/Endpoints/TransacaoEndpoints.cs
public static class TransacaoEndpoints
{
    public static void MapTransacaoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/transacoes")
            .WithTags("Transações")
            .WithOpenApi();

        // Endpoint para débito
        group.MapPost("/debito", async (
            DebitoRequest request,
            ITransactionProcessor processor,
            CancellationToken cancellationToken) =>
        {
            var transacao = new DebitoTransaction
            {
                NumeroContaDebito = request.NumeroContaDebito,
                Valor = request.Valor,
                Descricao = request.Descricao,
                Referencia = request.Referencia
            };

            var resultado = await processor.ExecuteAsync(transacao, cancellationToken);

            return resultado.IsSuccess 
                ? Results.Ok(new TransacaoResponse
                {
                    Sucesso = true,
                    Mensagem = "Débito realizado com sucesso!",
                    TransacaoId = transacao.Id,
                    Valor = request.Valor
                })
                : Results.BadRequest(new TransacaoResponse
                {
                    Sucesso = false,
                    Mensagem = resultado.Error,
                    TransacaoId = transacao.Id
                });
        })
        .WithName("RealizarDebito")
        .WithSummary("Realizar débito em conta");

        // Endpoint para transferência
        group.MapPost("/transferencia", async (
            TransferenciaRequest request,
            ITransactionProcessor processor,
            CancellationToken cancellationToken) =>
        {
            var transacao = new TransferenciaTransaction
            {
                NumeroContaOrigemDebito = request.NumeroContaOrigem,
                NumeroContaDestinoCredito = request.NumeroContaDestino,
                Valor = request.Valor,
                Descricao = request.Descricao,
                Tipo = request.Tipo
            };

            var resultado = await processor.ExecuteAsync(transacao, cancellationToken);

            return resultado.IsSuccess 
                ? Results.Ok(new TransacaoResponse
                {
                    Sucesso = true,
                    Mensagem = "Transferência realizada com sucesso!",
                    TransacaoId = transacao.Id,
                    Valor = request.Valor
                })
                : Results.BadRequest(new TransacaoResponse
                {
                    Sucesso = false,
                    Mensagem = resultado.Error,
                    TransacaoId = transacao.Id
                });
        })
        .WithName("RealizarTransferencia")
        .WithSummary("Realizar transferência entre contas");

        // Endpoint para pagamento de boleto
        group.MapPost("/pagamento-boleto", async (
            PagamentoBoletoRequest request,
            ITransactionProcessor processor,
            CancellationToken cancellationToken) =>
        {
            var transacao = new PagamentoBoletoTransaction
            {
                NumeroContaDebito = request.NumeroContaDebito,
                CodigoBarras = request.CodigoBarras,
                Valor = request.Valor,
                DataVencimento = request.DataVencimento,
                NomeBeneficiario = request.NomeBeneficiario
            };

            var resultado = await processor.ExecuteAsync(transacao, cancellationToken);

            return resultado.IsSuccess 
                ? Results.Ok(new TransacaoResponse
                {
                    Sucesso = true,
                    Mensagem = "Pagamento realizado com sucesso!",
                    TransacaoId = transacao.Id,
                    Valor = request.Valor
                })
                : Results.BadRequest(new TransacaoResponse
                {
                    Sucesso = false,
                    Mensagem = resultado.Error,
                    TransacaoId = transacao.Id
                });
        })
        .WithName("PagarBoleto")
        .WithSummary("Realizar pagamento de boleto");

        // Endpoint para consultar transação por token
        group.MapGet("/token/{token}", async (
            string token,
            ITransactionTokenService tokenService,
            CancellationToken cancellationToken) =>
        {
            var resultado = await tokenService.RecoverTransactionAsync(token);

            return resultado.IsSuccess 
                ? Results.Ok(new ConsultaTokenResponse
                {
                    Sucesso = true,
                    TransacaoId = resultado.Value!.CorrelationId,
                    TipoTransacao = resultado.Value.Type,
                    DataCriacao = DateTime.Parse(resultado.Value.CreatedAt)
                })
                : Results.BadRequest(new ConsultaTokenResponse
                {
                    Sucesso = false,
                    Erro = resultado.Error
                });
        })
        .WithName("ConsultarPorToken")
        .WithSummary("Consultar transação por token");
    }
}
```

### DTOs de Request e Response

```csharp
// DTOs para requests
public record DebitoRequest(
    string NumeroContaDebito,
    decimal Valor,
    string Descricao,
    string? Referencia = null);

public record TransferenciaRequest(
    string NumeroContaOrigem,
    string NumeroContaDestino,
    decimal Valor,
    string Descricao,
    TipoTransferencia Tipo = TipoTransferencia.TED);

public record PagamentoBoletoRequest(
    string NumeroContaDebito,
    string CodigoBarras,
    decimal Valor,
    DateTime? DataVencimento = null,
    string? NomeBeneficiario = null);

// DTOs para responses
public record TransacaoResponse
{
    public bool Sucesso { get; init; }
    public string Mensagem { get; init; } = string.Empty;
    public string? TransacaoId { get; init; }
    public decimal? Valor { get; init; }
}

public record ConsultaTokenResponse
{
    public bool Sucesso { get; init; }
    public string? TransacaoId { get; init; }
    public string? TipoTransacao { get; init; }
    public DateTime? DataCriacao { get; init; }
    public string? Erro { get; init; }
}
```

## 🧪 Testes e Validação

### Exemplo de Teste de Integração

```csharp
[Fact]
public async Task DebitoTransaction_ComSaldoSuficiente_DeveRealizarDebito()
{
    // Arrange
    var request = new DebitoRequest(
        NumeroContaDebito: "12345-6",
        Valor: 100.00m,
        Descricao: "Teste de débito",
        Referencia: "TEST-001"
    );

    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/api/transacoes/debito", content);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    
    var responseContent = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<TransacaoResponse>(responseContent);
    
    result.Should().NotBeNull();
    result.Sucesso.Should().BeTrue();
    result.Valor.Should().Be(100.00m);
}
```

### Exemplos de Requisições

#### Débito
```bash
curl -X POST "https://localhost:7001/api/transacoes/debito" \
  -H "Content-Type: application/json" \
  -d '{
    "numeroContaDebito": "12345-6",
    "valor": 500.00,
    "descricao": "Débito de teste",
    "referencia": "DEB-001"
  }'
```

#### Transferência
```bash
curl -X POST "https://localhost:7001/api/transacoes/transferencia" \
  -H "Content-Type: application/json" \
  -d '{
    "numeroContaOrigem": "12345-6",
    "numeroContaDestino": "67890-1",
    "valor": 1000.00,
    "descricao": "Transferência para fornecedor",
    "tipo": 2
  }'
```

#### Pagamento de Boleto
```bash
curl -X POST "https://localhost:7001/api/transacoes/pagamento-boleto" \
  -H "Content-Type: application/json" \
  -d '{
    "numeroContaDebito": "12345-6",
    "codigoBarras": "34191790010104351004791020150008291070000002000",
    "valor": 250.00,
    "nomeBeneficiario": "Empresa ABC Ltda"
  }'
```

## 📊 Monitoramento

O SDK inclui observabilidade completa com:

- **Logs Estruturados**: Serilog com correlação de transações
- **Tracing Distribuído**: OpenTelemetry com Jaeger
- **Métricas**: Contadores e timers de transações
- **Health Checks**: Verificação de saúde de dependências

### Eventos de Domínio Automatizados

- `TransactionStartedEvent`: Início do processamento
- `TransactionCompletedEvent`: Sucesso na transação
- `TransactionFailedEvent`: Falha no processamento
- `TransactionCancelledEvent`: Cancelamento da transação

## 🚀 Próximos Passos

1. **Implementar Repositórios Reais**: Entity Framework ou Dapper
2. **Adicionar Validação Avançada**: FluentValidation
3. **Configurar CI/CD**: Pipeline de build e deploy
4. **Testes Abrangentes**: Cobertura completa de testes
5. **Documentação OpenAPI**: Swagger detalhado
6. **Performance**: Otimizações e benchmarks

## 🔧 Interfaces e Serviços Necessários

Para implementar uma API completa usando o BKS SDK, você precisará criar as seguintes interfaces e suas implementações:

### Repositórios

```csharp
// Infrastructure/Repositories/IContaRepository.cs
public interface IContaRepository
{
    Task<Conta?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<Conta?> GetByNumeroAsync(string numero, CancellationToken cancellationToken);
    Task UpdateAsync(Conta conta, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string numero, CancellationToken cancellationToken);
    Task<IEnumerable<Movimentacao>> GetMovimentacoesAsync(string contaId, CancellationToken cancellationToken);
}
```

### Serviços de Domínio

```csharp
// Infrastructure/Services/IBoletoService.cs
public interface IBoletoService
{
    Task<BoletoInfo> ValidarCodigoBarrasAsync(string codigoBarras, CancellationToken cancellationToken);
    Task<decimal> CalcularValorComJurosAsync(BoletoInfo boleto, CancellationToken cancellationToken);
    Task RegistrarPagamentoAsync(string codigoBarras, decimal valor, CancellationToken cancellationToken);
}

public record BoletoInfo
{
    public bool IsValid { get; init; }
    public decimal Valor { get; init; }
    public DateTime DataVencimento { get; init; }
    public string NomeBeneficiario { get; init; } = string.Empty;
    public string TipoBoleto { get; init; } = string.Empty;
    public string CodigoBarras { get; init; } = string.Empty;
}

// Infrastructure/Services/ITaxaService.cs
public interface ITaxaService
{
    Task<decimal> CalcularTaxaAsync(TipoTransferencia tipo, decimal valor, CancellationToken cancellationToken);
    Task<TaxaInfo> ObterTaxasAsync(TipoTransferencia tipo, CancellationToken cancellationToken);
}

public record TaxaInfo
{
    public TipoTransferencia Tipo { get; init; }
    public decimal TaxaFixa { get; init; }
    public decimal PercentualSobreValor { get; init; }
    public decimal ValorMinimoTaxa { get; init; }
    public decimal ValorMaximoTaxa { get; init; }
}
```

### Entidades de Domínio

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

        var saldoAnterior = Saldo;
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
            SaldoAnterior = saldoAnterior,
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

        var saldoAnterior = Saldo;
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
            SaldoAnterior = saldoAnterior,
            SaldoPosterior = Saldo
        });
    }

    public bool PodeSacar(decimal valor) => Ativa && Saldo >= valor;

    public void Inativar() => Ativa = false;
    public void Ativar() => Ativa = true;
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

### Implementação In-Memory para Desenvolvimento

```csharp
// Infrastructure/Repositories/InMemoryContaRepository.cs
public class InMemoryContaRepository : IContaRepository
{
    private static readonly ConcurrentDictionary<string, Conta> _contas = new();
    private readonly ILogger<InMemoryContaRepository> _logger;

    static InMemoryContaRepository()
    {
        // Dados de exemplo para desenvolvimento
        var conta1 = new Conta("12345-6", "João Silva");
        conta1.Creditar(10000, "Saldo inicial");
        
        var conta2 = new Conta("67890-1", "Maria Santos");
        conta2.Creditar(5000, "Saldo inicial");
        
        var conta3 = new Conta("11111-1", "Empresa ABC Ltda");
        conta3.Creditar(50000, "Saldo inicial");

        _contas.TryAdd(conta1.Id, conta1);
        _contas.TryAdd(conta2.Id, conta2);
        _contas.TryAdd(conta3.Id, conta3);
    }

    public InMemoryContaRepository(ILogger<InMemoryContaRepository> logger)
    {
        _logger = logger;
    }

    public async Task<Conta?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken); // Simula latência de BD
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

    public async Task<IEnumerable<Movimentacao>> GetMovimentacoesAsync(string contaId, CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
        var conta = await GetByIdAsync(contaId, cancellationToken);
        return conta?.Movimentacoes ?? Enumerable.Empty<Movimentacao>();
    }
}
```

### Implementação Simulada do BoletoService

```csharp
// Infrastructure/Services/SimulatedBoletoService.cs
public class SimulatedBoletoService : IBoletoService
{
    private readonly ILogger<SimulatedBoletoService> _logger;

    public SimulatedBoletoService(ILogger<SimulatedBoletoService> logger)
    {
        _logger = logger;
    }

    public async Task<BoletoInfo> ValidarCodigoBarrasAsync(string codigoBarras, CancellationToken cancellationToken)
    {
        await Task.Delay(200, cancellationToken); // Simula consulta externa

        // Validação básica do tamanho
        if (string.IsNullOrWhiteSpace(codigoBarras) || codigoBarras.Length != 47)
        {
            return new BoletoInfo { IsValid = false };
        }

        // Simular diferentes cenários baseado no código
        var valor = ExtrairValor(codigoBarras);
        var dataVencimento = ExtrairDataVencimento(codigoBarras);
        var beneficiario = DeterminarBeneficiario(codigoBarras);

        var boletoInfo = new BoletoInfo
        {
            IsValid = true,
            Valor = valor,
            DataVencimento = dataVencimento,
            NomeBeneficiario = beneficiario,
            TipoBoleto = "Cobrança Registrada",
            CodigoBarras = codigoBarras
        };

        _logger.LogInformation("Boleto validado: {Beneficiario}, Valor: {Valor}, Vencimento: {Vencimento}",
            beneficiario, valor, dataVencimento);

        return boletoInfo;
    }

    public async Task<decimal> CalcularValorComJurosAsync(BoletoInfo boleto, CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);

        if (boleto.DataVencimento >= DateTime.Today)
            return boleto.Valor;

        var diasAtraso = (DateTime.Today - boleto.DataVencimento).Days;
        
        // Multa de 2% + juros de 0.033% ao dia (1% ao mês)
        var multa = boleto.Valor * 0.02m;
        var juros = boleto.Valor * 0.00033m * diasAtraso;
        
        var valorComJuros = boleto.Valor + multa + juros;

        _logger.LogInformation("Valor com juros calculado: Original: {Original}, Com juros: {ComJuros}, Dias atraso: {Dias}",
            boleto.Valor, valorComJuros, diasAtraso);

        return Math.Round(valorComJuros, 2);
    }

    public async Task RegistrarPagamentoAsync(string codigoBarras, decimal valor, CancellationToken cancellationToken)
    {
        await Task.Delay(300, cancellationToken); // Simula registro no sistema do beneficiário
        
        _logger.LogInformation("Pagamento registrado no sistema do beneficiário: Código {Codigo}, Valor {Valor}",
            codigoBarras, valor);
    }

    private decimal ExtrairValor(string codigoBarras)
    {
        // Em um código de barras real, o valor estaria em posições específicas
        // Para simulação, vamos extrair baseado nos últimos dígitos
        var valorStr = codigoBarras.Substring(37, 10);
        
        // Converte centavos para reais
        if (long.TryParse(valorStr, out var centavos))
        {
            return centavos / 100m;
        }
        
        // Valor padrão para testes
        return 250.00m;
    }

    private DateTime ExtrairDataVencimento(string codigoBarras)
    {
        // Simulação: vencimento baseado no 6º dígito
        var digito = int.Parse(codigoBarras.Substring(5, 1));
        
        return digito switch
        {
            >= 7 => DateTime.Today.AddDays(-5), // Vencido
            >= 4 => DateTime.Today.AddDays(5),  // A vencer
            _ => DateTime.Today.AddDays(15)     // A vencer (mais tempo)
        };
    }

    private string DeterminarBeneficiario(string codigoBarras)
    {
        // Simular diferentes beneficiários baseado no início do código
        var prefixo = codigoBarras.Substring(0, 3);
        
        return prefixo switch
        {
            "341" => "Banco Itaú S/A",
            "001" => "Banco do Brasil S/A",
            "104" => "Caixa Econômica Federal",
            "237" => "Banco Bradesco S/A",
            "033" => "Banco Santander S/A",
            _ => "Empresa Exemplo Ltda"
        };
    }
}
```

### Implementação do TaxaService

```csharp
// Infrastructure/Services/TaxaService.cs
public class TaxaService : ITaxaService
{
    private readonly ILogger<TaxaService> _logger;
    private readonly ICacheProvider _cache;

    public TaxaService(ILogger<TaxaService> logger, ICacheProvider cache)
    {
        _logger = logger;
        _cache = cache;
    }

    public async Task<decimal> CalcularTaxaAsync(TipoTransferencia tipo, decimal valor, CancellationToken cancellationToken)
    {
        // Buscar taxa no cache primeiro
        var cacheKey = $"taxa_{tipo}_{GetFaixaValor(valor)}";
        var taxaCached = await _cache.GetAsync(cacheKey);
        
        if (taxaCached != null && decimal.TryParse(taxaCached, out var taxaCache))
        {
            _logger.LogInformation("Taxa obtida do cache: {Tipo} = {Taxa}", tipo, taxaCache);
            return taxaCache;
        }

        await Task.Delay(50, cancellationToken); // Simula consulta de tabela de taxas

        var taxa = tipo switch
        {
            TipoTransferencia.PIX => 0m, // PIX é gratuito
            TipoTransferencia.TED => CalcularTaxaTED(valor),
            TipoTransferencia.DOC => CalcularTaxaDOC(valor),
            _ => 0m
        };

        // Armazenar no cache por 1 hora
        await _cache.SetAsync(cacheKey, taxa.ToString(), TimeSpan.FromHours(1));

        _logger.LogInformation("Taxa calculada para {Tipo}: R$ {Taxa} (Valor: R$ {Valor})", 
            tipo, taxa, valor);

        return taxa;
    }

    public async Task<TaxaInfo> ObterTaxasAsync(TipoTransferencia tipo, CancellationToken cancellationToken)
    {
        await Task.Delay(30, cancellationToken);

        return tipo switch
        {
            TipoTransferencia.PIX => new TaxaInfo
            {
                Tipo = tipo,
                TaxaFixa = 0m,
                PercentualSobreValor = 0m,
                ValorMinimoTaxa = 0m,
                ValorMaximoTaxa = 0m
            },
            TipoTransferencia.TED => new TaxaInfo
            {
                Tipo = tipo,
                TaxaFixa = 15.90m,
                PercentualSobreValor = 0m,
                ValorMinimoTaxa = 15.90m,
                ValorMaximoTaxa = 35.90m
            },
            TipoTransferencia.DOC => new TaxaInfo
            {
                Tipo = tipo,
                TaxaFixa = 12.90m,
                PercentualSobreValor = 0m,
                ValorMinimoTaxa = 12.90m,
                ValorMaximoTaxa = 30.90m
            },
            _ => throw new ArgumentException($"Tipo de transferência não suportado: {tipo}")
        };
    }

    private decimal CalcularTaxaTED(decimal valor)
    {
        return valor switch
        {
            <= 5000 => 15.90m,
            <= 10000 => 25.90m,
            <= 50000 => 35.90m,
            _ => 45.90m
        };
    }

    private decimal CalcularTaxaDOC(decimal valor)
    {
        return valor switch
        {
            <= 5000 => 12.90m,
            <= 10000 => 20.90m,
            <= 50000 => 30.90m,
            _ => 40.90m
        };
    }

    private string GetFaixaValor(decimal valor)
    {
        return valor switch
        {
            <= 5000 => "ate_5k",
            <= 10000 => "5k_a_10k",
            <= 50000 => "10k_a_50k",
            _ => "acima_50k"
        };
    }
}
```

## 📈 Configuração Avançada

### Configuração do Program.cs Completa

```csharp
using bks.sdk.Core.Initialization;
using bks.sdk.Transactions;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configuração do BKS SDK
builder.Services.AddBKSSDK();

// Registro dos processadores de transação
builder.Services.AddScoped<ITransactionProcessor, DebitoProcessor>();
builder.Services.AddScoped<ITransactionProcessor, TransferenciaProcessor>();
builder.Services.AddScoped<ITransactionProcessor, PagamentoBoletoProcessor>();

// Registro dos repositórios e serviços
builder.Services.AddScoped<IContaRepository, InMemoryContaRepository>();
builder.Services.AddScoped<IBoletoService, SimulatedBoletoService>();
builder.Services.AddScoped<ITaxaService, TaxaService>();

// Configuração da API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BKS Transações API",
        Version = "v1.0.0",
        Description = "API para processamento de transações financeiras usando BKS SDK",
        Contact = new OpenApiContact
        {
            Name = "Equipe BKS",
            Email = "contato@bks.com",
            Url = new Uri("https://bks.com")
        },
        License = new OpenApiLicense
        {
            Name = "Proprietary",
            Url = new Uri("https://bks.com/license")
        }
    });

    // Configurar autorização JWT no Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando o esquema Bearer. Exemplo: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configuração de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://app.bks.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddCheck("redis", () => HealthCheckResult.Healthy())
    .AddCheck("database", () => HealthCheckResult.Healthy());

var app = builder.Build();

// Pipeline de middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BKS Transações API v1");
        c.RoutePrefix = string.Empty; // Swagger na raiz
        c.DisplayRequestDuration();
        c.EnableFilter();
        c.ShowExtensions();
    });
}

// CORS
app.UseCors("AllowFrontend");

// Middleware do BKS SDK (inclui autenticação, observabilidade, correlação)
app.UseBKSSDK();

// Health Checks
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                duration = x.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
});

// Endpoint raiz
app.MapGet("/", () => Results.Redirect("/swagger"))
   .ExcludeFromDescription();

// Endpoints de informações da API
app.MapGet("/info", () => new
{
    Application = "BKS Transações API",
    Version = "1.0.0",
    Environment = app.Environment.EnvironmentName,
    Timestamp = DateTime.UtcNow
})
.WithTags("Info")
.WithName("GetApiInfo")
.WithSummary("Informações da API");

// Mapeamento dos endpoints de transações
app.MapTransacaoEndpoints();

// Tratamento global de erros
app.UseExceptionHandler("/error");
app.Map("/error", (HttpContext context) =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    
    return Results.Problem(
        title: "Erro interno do servidor",
        detail: app.Environment.IsDevelopment() ? error?.Message : "Ocorreu um erro interno",
        statusCode: 500
    );
});

app.Run();
```

## 🔍 Exemplo de Uso com Event Handlers

### Handler de Eventos de Transação

```csharp
// Infrastructure/EventHandlers/TransactionEventHandler.cs
public class TransactionEventHandler
{
    private readonly ILogger<TransactionEventHandler> _logger;
    private readonly ICacheProvider _cache;

    public TransactionEventHandler(
        ILogger<TransactionEventHandler> logger,
        ICacheProvider cache)
    {
        _logger = logger;
        _cache = cache;
    }

    public async Task HandleTransactionStarted(TransactionStartedEvent @event)
    {
        _logger.LogInformation("Transação iniciada: {TransactionId} - Tipo: {Type}",
            @event.TransactionId, @event.TransactionType);

        // Armazenar no cache para consulta rápida
        await _cache.SetAsync(
            $"transaction_{@event.TransactionId}",
            JsonSerializer.Serialize(@event),
            TimeSpan.FromHours(24)
        );
    }

    public async Task HandleTransactionCompleted(TransactionCompletedEvent @event)
    {
        _logger.LogInformation("Transação concluída com sucesso: {TransactionId}",
            @event.TransactionId);

        // Atualizar status no cache
        await _cache.SetAsync(
            $"transaction_status_{@event.TransactionId}",
            "completed",
            TimeSpan.FromDays(7)
        );

        // Aqui você poderia enviar notificações, emails, etc.
    }

    public async Task HandleTransactionFailed(TransactionFailedEvent @event)
    {
        _logger.LogError("Transação falhou: {TransactionId} - Erro: {Error}",
            @event.TransactionId, @event.Error);

        // Registrar para análise posterior
        await _cache.SetAsync(
            $"transaction_error_{@event.TransactionId}",
            JsonSerializer.Serialize(@event),
            TimeSpan.FromDays(30)
        );
    }
}
```

### Configuração dos Event Handlers

```csharp
// No Program.cs, adicionar após a configuração do SDK:
builder.Services.AddScoped<TransactionEventHandler>();

// Após app.UseBKSSDK(), configurar os handlers:
var eventBroker = app.Services.GetRequiredService<IEventBroker>();
var eventHandler = app.Services.GetRequiredService<TransactionEventHandler>();

await eventBroker.SubscribeAsync<TransactionStartedEvent>(eventHandler.HandleTransactionStarted);
await eventBroker.SubscribeAsync<TransactionCompletedEvent>(eventHandler.HandleTransactionCompleted);
await eventBroker.SubscribeAsync<TransactionFailedEvent>(eventHandler.HandleTransactionFailed);
```

## 📊 Métricas e Monitoramento Avançado

### Métricas Customizadas

```csharp
// Infrastructure/Metrics/TransactionMetrics.cs
public class TransactionMetrics
{
    private readonly ILogger<TransactionMetrics> _logger;
    
    public TransactionMetrics(ILogger<TransactionMetrics> logger)
    {
        _logger = logger;
    }

    public void RecordTransactionProcessed(string transactionType, bool success, TimeSpan duration)
    {
        _logger.LogInformation("Métrica registrada: Tipo={Type}, Sucesso={Success}, Duração={Duration}ms",
            transactionType, success, duration.TotalMilliseconds);
    }

    public void RecordTransactionVolume(string transactionType, decimal amount)
    {
        _logger.LogInformation("Volume de transação: Tipo={Type}, Valor={Amount}",
            transactionType, amount);
    }
}
```

## 🔐 Segurança e Auditoria

### Auditoria de Transações

```csharp
// Infrastructure/Audit/TransactionAuditService.cs
public class TransactionAuditService
{
    private readonly ILogger<TransactionAuditService> _logger;
    private readonly ICacheProvider _cache;

    public TransactionAuditService(
        ILogger<TransactionAuditService> logger,
        ICacheProvider cache)
    {
        _logger = logger;
        _cache = cache;
    }

    public async Task LogTransactionAttempt(BaseTransaction transaction, string userAgent, string ipAddress)
    {
        var auditLog = new
        {
            TransactionId = transaction.Id,
            Type = transaction.GetType().Name,
            Timestamp = DateTime.UtcNow,
            UserAgent = userAgent,
            IpAddress = ipAddress,
            CorrelationId = transaction.CorrelationId
        };

        _logger.LogInformation("Tentativa de transação: {@AuditLog}", auditLog);

        // Armazenar para auditoria
        await _cache.SetAsync(
            $"audit_{transaction.Id}",
            JsonSerializer.Serialize(auditLog),
            TimeSpan.FromDays(365) // Manter por 1 ano
        );
    }
}
```

## 📚 Testes Avançados

### Testes de Integração Completos

```csharp
// Tests/Integration/TransactionIntegrationTests.cs
public class TransactionIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TransactionIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Theory]
    [InlineData("12345-6", 100.00, "Débito teste")]
    [InlineData("67890-1", 250.50, "Outro débito")]
    public async Task DebitoTransaction_ComParametrosValidos_DeveProcessarComSucesso(
        string conta, decimal valor, string descricao)
    {
        // Arrange
        var request = new DebitoRequest(conta, valor, descricao);
        var content = CreateJsonContent(request);

        // Act
        var response = await _client.PostAsync("/api/transacoes/debito", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await DeserializeResponse<TransacaoResponse>(response);
        result.Sucesso.Should().BeTrue();
        result.Valor.Should().Be(1000.00m);
    }

    [Fact]
    public async Task PagamentoBoleto_ComCodigoValido_DeveProcessarPagamento()
    {
        // Arrange
        var request = new PagamentoBoletoRequest(
            "12345-6", 
            "34191790010104351004791020150008291070000025000", 
            250.00m);
        var content = CreateJsonContent(request);

        // Act
        var response = await _client.PostAsync("/api/transacoes/pagamento-boleto", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await DeserializeResponse<TransacaoResponse>(response);
        result.Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task DebitoTransaction_ComSaldoInsuficiente_DeveRetornarErro()
    {
        // Arrange - valor maior que o saldo disponível
        var request = new DebitoRequest("12345-6", 999999.00m, "Débito inválido");
        var content = CreateJsonContent(request);

        // Act
        var response = await _client.PostAsync("/api/transacoes/debito", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var result = await DeserializeResponse<TransacaoResponse>(response);
        result.Sucesso.Should().BeFalse();
        result.Mensagem.Should().Contain("insuficiente");
    }

    private StringContent CreateJsonContent<T>(T obj)
    {
        var json = JsonSerializer.Serialize(obj);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private async Task<T> DeserializeResponse<T>(HttpResponse response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }
}
```

### Testes de Performance

```csharp
// Tests/Performance/TransactionPerformanceTests.cs
public class TransactionPerformanceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TransactionPerformanceTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task DebitoTransaction_Concorrencia_DeveManterConsistencia()
    {
        // Arrange
        const int numberOfRequests = 50;
        const decimal valorPorDebito = 10.00m;
        
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Executar múltiplas transações simultâneas
        for (int i = 0; i < numberOfRequests; i++)
        {
            var request = new DebitoRequest("12345-6", valorPorDebito, $"Débito concorrente {i}");
            var content = CreateJsonContent(request);
            
            tasks.Add(_client.PostAsync("/api/transacoes/debito", content));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        var sucessos = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        var falhas = responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest);

        // Verificar que pelo menos algumas transações foram processadas
        sucessos.Should().BeGreaterThan(0);
        
        // O total deve ser igual ao número de requisições
        (sucessos + falhas).Should().Be(numberOfRequests);

        // Log para análise
        Console.WriteLine($"Sucessos: {sucessos}, Falhas: {falhas}");
    }

    [Fact]
    public async Task TransferenciaTransaction_TempoResposta_DeveFicarDentroDosLimites()
    {
        // Arrange
        var request = new TransferenciaRequest(
            "12345-6", "67890-1", 100.00m, "Teste performance", TipoTransferencia.PIX);
        var content = CreateJsonContent(request);

        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.PostAsync("/api/transacoes/transferencia", content);

        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000); // Menos de 2 segundos
    }

    private StringContent CreateJsonContent<T>(T obj)
    {
        var json = JsonSerializer.Serialize(obj);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
```

## 🚀 Deploy e Produção

### Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["TransacoesAPI/TransacoesAPI.csproj", "TransacoesAPI/"]
RUN dotnet restore "TransacoesAPI/TransacoesAPI.csproj"
COPY . .
WORKDIR "/src/TransacoesAPI"
RUN dotnet build "TransacoesAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TransacoesAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Configuração de variáveis de ambiente
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "TransacoesAPI.dll"]
```

### docker-compose.yml

```yaml
version: '3.8'

services:
  transacoes-api:
    build: .
    ports:
      - "5000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - bkssdk__Redis__ConnectionString=redis:6379
      - bkssdk__EventBroker__ConnectionString=amqp://guest:guest@rabbitmq:5672/
    depends_on:
      - redis
      - rabbitmq
      - jaeger
    networks:
      - bks-network

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data
    networks:
      - bks-network

  rabbitmq:
    image: rabbitmq:3-management-alpine
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      - RABBITMQ_DEFAULT_USER=guest
      - RABBITMQ_DEFAULT_PASS=guest
    volumes:
      - rabbitmq-data:/var/lib/rabbitmq
    networks:
      - bks-network

  jaeger:
    image: jaegertracing/all-in-one:latest
    ports:
      - "16686:16686"
      - "14268:14268"
    environment:
      - COLLECTOR_OTLP_ENABLED=true
    networks:
      - bks-network

  prometheus:
    image: prom/prometheus:latest
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus-data:/prometheus
    networks:
      - bks-network

  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
    volumes:
      - grafana-data:/var/lib/grafana
    networks:
      - bks-network

volumes:
  redis-data:
  rabbitmq-data:
  prometheus-data:
  grafana-data:

networks:
  bks-network:
    driver: bridge
```

## 🔧 Configuração de Produção

### appsettings.Production.json

```json
{
  "bkssdk": {
    "LicenseKey": "${BKS_LICENSE_KEY}",
    "ApplicationName": "TransacoesAPI-Prod",
    "Redis": {
      "ConnectionString": "${REDIS_CONNECTION_STRING}",
      "InstanceName": "transacoes-prod",
      "Database": 0
    },
    "Jwt": {
      "SecretKey": "${JWT_SECRET_KEY}",
      "Issuer": "BKS-TransacoesAPI",
      "Audience": "bks-clients",
      "ExpirationInMinutes": 120
    },
    "EventBroker": {
      "BrokerType": "RabbitMQ",
      "ConnectionString": "${RABBITMQ_CONNECTION_STRING}",
      "AdditionalSettings": {
        "ExchangeName": "bks-transacoes-prod",
        "QueuePrefix": "prod",
        "RetryAttempts": "5",
        "RetryDelay": "10000"
      }
    },
    "Observability": {
      "ServiceName": "BKS-TransacoesAPI",
      "ServiceVersion": "1.0.0",
      "JaegerEndpoint": "${JAEGER_ENDPOINT}"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "System": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

## 📋 Lista de Verificação para Produção

### Segurança
- [ ] Variáveis de ambiente para dados sensíveis
- [ ] HTTPS configurado com certificados válidos
- [ ] Rate limiting implementado
- [ ] Validação rigorosa de entrada
- [ ] Logs não expõem dados sensíveis
- [ ] Autenticação JWT configurada

### Performance
- [ ] Cache Redis configurado
- [ ] Connection pooling otimizado
- [ ] Timeouts apropriados
- [ ] Compressão de resposta habilitada
- [ ] Pagination em listagens
- [ ] Índices de banco otimizados

### Monitoramento
- [ ] Health checks configurados
- [ ] Métricas sendo coletadas
- [ ] Alertas configurados
- [ ] Logs centralizados
- [ ] Tracing distribuído ativo
- [ ] Dashboard de monitoramento

### Backup e Recuperação
- [ ] Backup automático de dados
- [ ] Plano de recuperação de desastres
- [ ] Testes de restore
- [ ] Documentação de procedimentos

## 🌟 Melhores Práticas

### Estrutura de Projeto Recomendada

```
TransacoesAPI/
├── src/
│   ├── TransacoesAPI/                    # Camada de apresentação
│   │   ├── Controllers/
│   │   ├── Endpoints/
│   │   ├── Middlewares/
│   │   ├── DTOs/
│   │   └── Program.cs
│   ├── TransacoesAPI.Domain/             # Camada de domínio
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   ├── Services/
│   │   └── Events/
│   ├── TransacoesAPI.Application/        # Camada de aplicação
│   │   ├── UseCases/
│   │   ├── Handlers/
│   │   ├── DTOs/
│   │   └── Validators/
│   └── TransacoesAPI.Infrastructure/     # Camada de infraestrutura
│       ├── Repositories/
│       ├── Services/
│       ├── EventHandlers/
│       └── Configuration/
├── tests/
│   ├── TransacoesAPI.UnitTests/
│   ├── TransacoesAPI.IntegrationTests/
│   └── TransacoesAPI.PerformanceTests/
├── docs/
│   ├── api/
│   ├── architecture/
│   └── deployment/
└── docker/
    ├── Dockerfile
    ├── docker-compose.yml
    └── docker-compose.prod.yml
```

### Princípios de Design

1. **Single Responsibility Principle**: Cada classe tem uma única responsabilidade
2. **Open/Closed Principle**: Extensível para novos tipos de transação sem modificar código existente
3. **Dependency Inversion**: Dependências abstraídas através de interfaces
4. **Command Query Separation**: Separação clara entre comandos e consultas
5. **Domain Events**: Comunicação assíncrona entre bounded contexts

### Padrões de Validação

```csharp
// Application/Validators/DebitoTransactionValidator.cs
public class DebitoTransactionValidator : AbstractValidator<DebitoRequest>
{
    public DebitoTransactionValidator()
    {
        RuleFor(x => x.NumeroContaDebito)
            .NotEmpty().WithMessage("Número da conta é obrigatório")
            .Matches(@"^\d{5}-\d$").WithMessage("Formato de conta inválido");

        RuleFor(x => x.Valor)
            .GreaterThan(0).WithMessage("Valor deve ser maior que zero")
            .LessThanOrEqualTo(100000).WithMessage("Valor máximo excedido");

        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("Descrição é obrigatória")
            .MaximumLength(200).WithMessage("Descrição muito longa");
    }
}
```

## 📚 Recursos Adicionais

### Links Úteis

- [Documentação .NET 8](https://docs.microsoft.com/dotnet/core/whats-new/dotnet-8)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [Serilog Documentation](https://serilog.net/)
- [Minimal APIs](https://docs.microsoft.com/aspnet/core/fundamentals/minimal-apis)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

### Livros Recomendados

- "Clean Architecture" - Robert C. Martin
- "Domain-Driven Design" - Eric Evans
- "Implementing Domain-Driven Design" - Vaughn Vernon
- "Patterns of Enterprise Application Architecture" - Martin Fowler

### Ferramentas de Desenvolvimento

- **IDE**: Visual Studio 2022, JetBrains Rider, VS Code
- **Testing**: xUnit, FluentAssertions, Testcontainers
- **Monitoring**: Jaeger, Prometheus, Grafana
- **API Testing**: Postman, Insomnia, REST Client
- **Documentation**: Swagger/OpenAPI, Markdown

## 🤝 Contribuição

### Guidelines de Contribuição

1. **Fork** o repositório
2. **Crie** uma branch para sua feature (`git checkout -b feature/nova-funcionalidade`)
3. **Implemente** sua funcionalidade seguindo os padrões do projeto
4. **Adicione** testes para cobrir sua implementação
5. **Execute** todos os testes (`dotnet test`)
6. **Commit** suas mudanças (`git commit -m 'Adiciona nova funcionalidade'`)
7. **Push** para sua branch (`git push origin feature/nova-funcionalidade`)
8. **Abra** um Pull Request

### Padrões de Código

- Seguir convenções do C# (.NET)
- Usar nomenclatura em inglês para código
- Documentar métodos públicos com XML comments
- Manter cobertura de testes acima de 80%
- Usar async/await para operações I/O

## 📞 Suporte

### Canais de Suporte

- 📧 **Email**: fabio@backside.com
- 📖 **Documentação**: Portal interno BKS
- 🧪 **Exemplos**: [Repositório de exemplos](https://github.com/bks-sdk/examples)

### FAQ

**P: O SDK funciona com .NET 6 ou .NET 7?**
R: O SDK foi desenvolvido especificamente para .NET 8. Para versões anteriores, consulte nossa equipe de suporte.

**P: Posso usar outros sistemas de cache além do Redis?**
R: Sim, o SDK fornece uma interface `ICacheProvider` que pode ser implementada para qualquer sistema de cache.

**P: Como configuro o SDK para usar Kafka em vez de RabbitMQ?**
R: Altere o valor de `BrokerType` para `"Kafka"` na configuração e forneça a connection string apropriada.

**P: O SDK suporta transações distribuídas?**
R: Atualmente o SDK foca em transações locais com eventos assíncronos. Para transações distribuídas, consulte nossa roadmap.

---

## 📜 Licença

Este SDK é propriedade da BKS e está licenciado sob termos proprietários. 
Consulte o arquivo `LICENSE` para mais detalhes sobre uso e distribuição.

---

**BKS SDK v1.0.2** - Desenvolvido com ❤️ pela equipe BKS para acelerar o desenvolvimento de aplicações financeiras robustas e escaláveis.

**Última atualização**: Dezembro 2024Response>(response);
        result.Sucesso.Should().BeTrue();
        result.Valor.Should().Be(valor);
        result.TransacaoId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TransferenciaTransaction_EntreContasValidas_DeveProcessarComTaxa()
    {
        // Arrange
        var request = new TransferenciaRequest(
            "12345-6", "67890-1", 1000.00m, "Transferência teste", TipoTransferencia.TED);
        var content = CreateJsonContent(request);

        // Act
        var response = await _client.PostAsync("/api/transacoes/transferencia", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await DeserializeResponse<Transacao