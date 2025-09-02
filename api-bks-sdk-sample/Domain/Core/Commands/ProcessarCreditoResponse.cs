using Domain.Core.Entities;

namespace Domain.Core.Commands
{
    public record ProcessarCreditoResponse
    {
        public bool Sucesso { get; init; }
        public string? Mensagem { get; init; }
        public decimal NovoSaldo { get; init; }
        public string ContaId { get; init; } = string.Empty;
        public int NumeroConta { get; init; } 
        public string MovimentacaoId { get; init; } = string.Empty;
        public DateTime DataProcessamento { get; init; } = DateTime.UtcNow;
        public decimal ValorCreditado { get; init; }
        public decimal SaldoAnterior { get; init; }
        public string TitularConta { get; init; } = string.Empty;
        public Dictionary<string, object> DadosAdicionais { get; init; } = new();

        // Factory methods para criar responses
        public static ProcessarCreditoResponse Concluido(
            Conta conta,
            decimal valorCreditado,
            decimal saldoAnterior,
            string? movimentacaoId = null)
        {
            return new ProcessarCreditoResponse
            {
                Sucesso = true,
                Mensagem = "Crédito processado com sucesso",
                NovoSaldo = conta.Saldo,
                ContaId = conta.Id,
                NumeroConta = conta.Numero,
                MovimentacaoId = movimentacaoId ?? Guid.NewGuid().ToString(),
                ValorCreditado = valorCreditado,
                SaldoAnterior = saldoAnterior,
                TitularConta = conta.Titular,
                DataProcessamento = DateTime.UtcNow
            };
        }

        public static ProcessarCreditoResponse Falha(string mensagem)
        {
            return new ProcessarCreditoResponse
            {
                Sucesso = false,
                Mensagem = mensagem,
                DataProcessamento = DateTime.UtcNow
            };
        }
    }

}
