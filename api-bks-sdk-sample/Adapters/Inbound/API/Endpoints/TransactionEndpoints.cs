
using Adapters.Inbound.API.DTOs.Request;
using Adapters.Inbound.API.DTOs.Response;
using bks.sdk.Core.Pipeline;
using bks.sdk.Processing.Mediator.Abstractions;
using Domain.Core.Commands;
using Domain.Core.Entities;
using Domain.Core.Ports.Outbound;
using Domain.Core.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace Adapters.Inbound.API.Endpoints
{
    public static partial class TransactionEndpoints
    {
        public static void AddTransactionEndpoints(this WebApplication app)
        {
            var transactiongroup = app.MapGroup("api/sdk/v1/transactions")
                                      .WithTags("Transactions")
                                      .RequireAuthorization();


            // Endpoint de CRÉDITO usando MEDIATOR PATTERN
            transactiongroup.MapPost("/credito", async (
                CreditoRequestDto request,
                IBKSMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new ProcessarCreditoCommand
                {
                    NumeroContaCredito = request.NumeroConta,
                    Valor = request.Valor,
                    Descricao = request.Descricao,
                    Referencia = request.Referencia,
                    RequestId = Guid.NewGuid().ToString("N"),
                    CreatedAt = DateTime.UtcNow
                };

                var resultado = await mediator.SendAsync(command, cancellationToken);

                if (resultado.IsSuccess)
                {
                    return Results.Ok(new TransacaoResponseDto
                    {
                        Sucesso = true,
                        Mensagem = "Crédito processado com sucesso via Mediator!",
                        TransacaoId = command.RequestId,
                        Valor = request.Valor,
                        NovoSaldo = resultado.Value?.NovoSaldo,
                        ProcessadoPor = "Mediator Pattern"
                    });
                }

                return Results.BadRequest(new TransacaoResponseDto
                {
                    Sucesso = false,
                    Mensagem = resultado.Error,
                    TransacaoId = command.RequestId
                });
            })
            .WithName("ProcessarCredito")
            .WithSummary("Processar crédito usando Mediator Pattern")
            .WithDescription("Este endpoint utiliza o padrão Mediator do BKS SDK para processar transações de crédito");

            // Endpoint de DÉBITO usando TRANSACTION PROCESSOR
            transactiongroup.MapPost("/debito", async (
                DebitoRequestDto request,
                IPipelineExecutor pipelineExecutor,
                CancellationToken cancellationToken) =>
            {
                var transacao = new DebitoTransaction
                {
                    NumeroConta = request.NumeroConta,
                    Valor = request.Valor,
                    Descricao = request.Descricao,
                    Referencia = request.Referencia
                };

                // Usar o Pipeline Executor que internamente usará o Transaction Processor
                var resultado = await pipelineExecutor.ExecuteAsync<DebitoTransaction, DebitoResponse>(
                    transacao, cancellationToken);

                if (resultado.IsSuccess)
                {
                    return Results.Ok(new TransacaoResponseDto
                    {
                        Sucesso = true,
                        Mensagem = "Débito processado com sucesso via Transaction Processor!",
                        TransacaoId = transacao.Id,
                        Valor = request.Valor,
                        NovoSaldo = resultado.Value?.NovoSaldo,
                        ProcessadoPor = "Transaction Processor Pattern"
                    });
                }

                return Results.BadRequest(new TransacaoResponseDto
                {
                    Sucesso = false,
                    Mensagem = resultado.Error,
                    TransacaoId = transacao.Id
                });
            })
            .WithName("ProcessarDebito")
            .WithSummary("Processar débito usando Transaction Processor");
            

            transactiongroup.MapGet("/conta/{numeroConta}", async (
               int numeroConta,
               IContaRepository contaRepository,
               CancellationToken cancellationToken) =>
                {
                    var conta = await contaRepository.GetByNumeroAsync(numeroConta, cancellationToken);

                    if (conta == null)
                    {
                        return Results.NotFound(new { Mensagem = "Conta não encontrada" });
                    }

                    return Results.Ok(new
                    {
                        NumeroConta = conta.Numero,
                        Titular = conta.Titular,
                        Saldo = conta.Saldo,
                        Ativa = conta.Ativa,
                        DataCriacao = conta.DataCriacao,
                        UltimasMovimentacoes = conta.Movimentacoes
                            .OrderByDescending(m => m.DataMovimentacao)
                            .Take(5)
                            .Select(m => new
                            {
                                m.Id,
                                m.Tipo,
                                m.Valor,
                                m.Descricao,
                                m.DataMovimentacao,
                                m.SaldoAnterior,
                                m.SaldoPosterior
                            })
                    });
                })
            .WithName("ConsultarConta")
            .WithSummary("Consultar informações da conta")
            .WithDescription("Consulta o saldo e movimentações de uma conta");


            transactiongroup.MapPost("/conta", async (
                 CriarContaRequestDto request,
                 IContaRepository contaRepository,
                 CancellationToken cancellationToken) =>
             {
                 var conta = new Conta(request.Numero, request.Titular);

                 if (request.SaldoInicial > 0)
                 {
                     conta.Creditar(request.SaldoInicial, "Saldo inicial");
                 }

                 await contaRepository.CreateAsync(conta, cancellationToken);

                 return Results.Created($"/api/transactions/conta/{conta.Numero}", new
                 {
                     conta.Id,
                     conta.Numero,
                     conta.Titular,
                     conta.Saldo,
                     conta.Ativa,
                     conta.DataCriacao,
                     Mensagem = "Conta criada com sucesso"
                 });
             })
            .WithName("CriarConta")
            .WithSummary("Criar nova conta")
            .WithDescription("Cria uma nova conta para testes");

        }

    }
}
