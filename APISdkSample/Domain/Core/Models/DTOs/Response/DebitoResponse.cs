using Domain.Core.Models.Entities;
using Domain.Core.Models.Results;

namespace Domain.Core.Models.DTOs.Response
{
    public record DebitoResponse
    {
        public bool Sucesso { get; init; }
        public string Mensagem { get; init; } = string.Empty;
        public string? TransacaoId { get; init; }
        public string? ContaId { get; init; }
        public string? NumeroConta { get; init; }
        public string? TitularConta { get; init; }
        public decimal? ValorDebitado { get; init; }
        public decimal? SaldoAnterior { get; init; }
        public decimal? NovoSaldo { get; init; }
        public DateTime? DataProcessamento { get; init; }
        public string? NumeroComprovante { get; init; }
        public MovimentacaoInfo? Movimentacao { get; init; }
        public string? Referencia { get; init; }

        public static DebitoResponse FromSuccess(DebitoResult resultado)
        {
            return new DebitoResponse
            {
                Sucesso = true,
                Mensagem = "Débito processado com sucesso",
                TransacaoId = resultado.TransacaoId,
                ContaId = resultado.ContaId,
                NumeroConta = resultado.NumeroConta,
                TitularConta = resultado.TitularConta,
                ValorDebitado = resultado.ValorDebitado,
                SaldoAnterior = resultado.SaldoAnterior,
                NovoSaldo = resultado.NovoSaldo,
                DataProcessamento = resultado.DataProcessamento,
                NumeroComprovante = $"DEB{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}",
                Movimentacao = resultado.UltimaMovimentacao,
                Referencia = resultado.Referencia
            };
        }

        public static DebitoResponse FromFailure(string transacaoId, string erro)
        {
            return new DebitoResponse
            {
                Sucesso = false,
                Mensagem = erro,
                TransacaoId = transacaoId
            };
        }
    }
}
