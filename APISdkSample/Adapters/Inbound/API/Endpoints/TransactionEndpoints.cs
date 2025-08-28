using bks.sdk.Transactions;
using Domain.Core.Interfaces.Outbound;
using Domain.Core.Models.DTOs.Request;
using Domain.Core.Models.DTOs.Response;
using Domain.Core.Models.Entities;
using Domain.Core.Models.Results;
using Domain.Core.Transactions;
using Domain.Processors;
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

  
            transactiongroup.MapPost("/debit", async (
                              [FromBody] DebitoRequest request,
                              [FromServices]  ITransactionProcessor<DebitoResult> processor,
                              HttpContext context,
                              CancellationToken cancellationToken) =>
            {
                try
                {

                    var transacao = DebitoTransaction.Create(request);

                    // Processar através do SDK (pipeline completa)
                    var resultado = await processor.ExecuteAsync(transacao, cancellationToken);

                    if (resultado.IsSuccess)
                    {
                        var response = DebitoResponse.FromSuccess(resultado.Value);
                        return Results.Ok(response);
                    }
                    else
                    {
                        var response = DebitoResponse.FromFailure(transacao.CorrelationId, resultado.Error ?? "Erro não especificado");
                        return Results.BadRequest(response);
                    }
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Erro interno",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            })
            .WithName("ProcessarDebito")
            .WithSummary("Processar transação de débito")
            .WithDescription(@"
            Processa uma transação de débito em conta corrente usando o bks sdk.
            
            **Pipeline de Processamento:**
            1. Validação da transação (formato, limites, dados obrigatórios)
            2. Pré-processamento (verificações de segurança, fraude, limites)
            3. Processamento principal (execução do débito + criação do resultado tipado)
            4. Pós-processamento (notificações, analytics, scores usando dados do resultado)
            
            **Vantagens do Resultado Tipado:**
            - ✅ Endpoint não precisa injetar repositório
            - ✅ Dados da conta já vem atualizados no resultado
            - ✅ Resposta construída diretamente do resultado do processador
            - ✅ Redução de consultas ao banco de dados
            - ✅ Melhor performance e menor acoplamento
            
            **Funcionalidades da Entidade Conta:**
            - Validação automática de valor > 0
            - Verificação de conta ativa
            - Verificação de saldo suficiente
            - Criação automática da movimentação
            - Controle de saldo anterior/posterior
            - Atualização da data da última movimentação")
            .Produces<DebitoResponse>(200)
            .Produces<DebitoResponse>(400)
            .Produces(500);


            transactiongroup.MapGet("/account/{numeroConta}/moviments", async (
                int numeroConta,
                [FromServices] IContaRepository contaRepository,
                DateTime? dataInicio,
                DateTime? dataFim,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var conta = await contaRepository.GetByNumeroAsync(numeroConta, cancellationToken);
                    if (conta == null)
                        return Results.NotFound(new { Mensagem = "Conta não encontrada" });

                    var movimentacoes = await contaRepository.GetMovimentacoesAsync(
                        conta.Id, dataInicio, dataFim, cancellationToken);

                    var response = new
                    {
                        NumeroConta = conta.Numero,
                        Titular = conta.Titular,
                        SaldoAtual = conta.Saldo,
                        TotalMovimentacoes = movimentacoes.Count(),
                        Movimentacoes = movimentacoes.Select(m => new
                        {
                            m.Id,
                            Tipo = m.Tipo.ToString(),
                            m.Valor,
                            m.Descricao,
                            m.Referencia,
                            m.DataMovimentacao,
                            m.SaldoAnterior,
                            m.SaldoPosterior
                        }).OrderByDescending(m => m.DataMovimentacao)
                    };

                    return Results.Ok(response);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Erro interno",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            })
            .WithName("ConsultarMovimentacoes")
            .WithSummary("Consultar histórico de movimentações da conta");


            transactiongroup.MapGet("/{transacaoId}/status", async (
            string transacaoId,
            [FromServices] ITransactionTokenService tokenService,
            CancellationToken cancellationToken) =>
            {
                var resultado = await tokenService.RecoverTransactionAsync<DebitoTransaction>(transacaoId);

                if (resultado.IsSuccess)
                {
                    return Results.Ok(new
                    {
                        TransacaoId = transacaoId,
                        Status = "Encontrada",
                        Transacao = resultado.Value
                    });
                }

                return Results.NotFound(new { Mensagem = "Transação não encontrada" });
            })
        .WithName("ConsultarStatusDebito")
        .WithSummary("Consultar status de transação de débito");
        }


    }
}
