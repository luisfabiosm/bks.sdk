using Domain.Core.Enums;

namespace Domain.Core.Models.Entities
{
    public record Conta
    {
        public string Id { get; private set; } = string.Empty;
        public int Numero { get; private set; }
        public string Titular { get; private set; } = string.Empty;
        public decimal Saldo { get; private set; }
        public bool Ativa { get; private set; }
        public DateTime DataCriacao { get; private set; }
        public DateTime? DataUltimaMovimentacao { get; private set; }

        private readonly List<MovimentacaoInfo> _movimentacoes = new();
        public IReadOnlyList<MovimentacaoInfo> Movimentacoes => _movimentacoes.AsReadOnly();

        public Conta(int numero, string titular)
        {
            Id = Guid.NewGuid().ToString();
            Numero = numero;
            Titular = titular;
            Saldo = 0;
            Ativa = true;
            DataCriacao = DateTime.UtcNow;
        }

        public void Debitar(decimal valor, string descricao = "", string referencia = "")
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

            _movimentacoes.Add(new MovimentacaoInfo
            {
                Id = Guid.NewGuid().ToString(),
                ContaId = Id,
                Tipo = TipoMovimentacao.Debito,
                Valor = valor,
                Descricao = descricao,
                Referencia = referencia,
                DataMovimentacao = DataUltimaMovimentacao.Value,
                SaldoAnterior = saldoAnterior,
                SaldoPosterior = Saldo
            });
        }

        public bool PodeSacar(decimal valor) => Ativa && Saldo >= valor;
    }
}
