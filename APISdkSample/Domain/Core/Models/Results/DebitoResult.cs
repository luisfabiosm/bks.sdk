using Domain.Core.Models.Entities;
using Domain.Core.Transactions;

namespace Domain.Core.Models.Results
{
    public record DebitoResult
    {
        public string TransacaoId { get; init; } = string.Empty;
        public string ContaId { get; init; } = string.Empty;
        public string NumeroConta { get; init; } = string.Empty;
        public string TitularConta { get; init; } = string.Empty;
        public decimal ValorDebitado { get; init; }
        public decimal SaldoAnterior { get; init; }
        public decimal NovoSaldo { get; init; }
        public DateTime DataProcessamento { get; init; }
        public MovimentacaoInfo UltimaMovimentacao { get; init; } = new();
        public string? Referencia { get; init; }
        public string Descricao { get; init; } = string.Empty;

        public static DebitoResult From(DebitoTransaction transacao, Conta conta)
        {
            var ultimaMovimentacao = conta.Movimentacoes.LastOrDefault();

            return new DebitoResult
            {
                TransacaoId = transacao.CorrelationId,
                ContaId = conta.Id,
                NumeroConta = conta.Numero,
                TitularConta = conta.Titular,
                ValorDebitado = transacao.Valor,
                SaldoAnterior = ultimaMovimentacao?.SaldoAnterior ?? conta.Saldo,
                NovoSaldo = conta.Saldo,
                DataProcessamento = DateTime.UtcNow,
                UltimaMovimentacao = ultimaMovimentacao != null ? new MovimentacaoInfo
                {
                    Id = ultimaMovimentacao.Id,
                    SaldoAnterior = ultimaMovimentacao.SaldoAnterior,
                    SaldoPosterior = ultimaMovimentacao.SaldoPosterior,
                    DataMovimentacao = ultimaMovimentacao.DataMovimentacao,
                    Referencia = ultimaMovimentacao.Referencia
                } : new MovimentacaoInfo(),
                Referencia = transacao.Referencia,
                Descricao = transacao.Descricao
            };
        }
    }
}

