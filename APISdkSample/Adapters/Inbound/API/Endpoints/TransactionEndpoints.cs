using bks.sdk.Transactions;
using Domain.Core.DTOs.Request;
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

  
            transactiongroup.MapPost("/entry/debit", async (
                              EntryDebitRequest request,
                              ITransactionProcessor processor,
                              CancellationToken cancellationToken) =>
            {
                var _transaction = new EntryDebitTransaction
                {
                    Branch = request.Branch,
                    AccountNumber = request.AccountNumber,
                    Value = request.Value,
                    Ref = request.Ref,
                    Detail = request.Detail
                };

                var resultado = await processor.ExecuteAsync(_transaction, cancellationToken);

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
            .WithName("Entry Debit Transaction")
            .WithSummary("Realizar lancamento a Débito");
        }
    }
}
