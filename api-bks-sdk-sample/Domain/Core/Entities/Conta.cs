
using Domain.Core.Enums;

namespace Domain.Core.Entities
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

        private readonly List<Movimentacao> _movimentacoes = new();
        public IReadOnlyList<Movimentacao> Movimentacoes => _movimentacoes.AsReadOnly();

        public Conta(int numero, string titular)
        {
            if (numero ==0)
                throw new ArgumentException("Número da conta é obrigatório", nameof(numero));

            if (string.IsNullOrWhiteSpace(titular))
                throw new ArgumentException("Titular da conta é obrigatório", nameof(titular));

            Id = Guid.NewGuid().ToString();
            Numero = numero;
            Titular = titular;
            Saldo = 0;
            Ativa = true;
            DataCriacao = DateTime.UtcNow;
        }

        public void Creditar(decimal valor, string descricao = "")
        {
            ValidarOperacao(valor, descricao);

            var saldoAnterior = Saldo;
            Saldo += valor;
            DataUltimaMovimentacao = DateTime.UtcNow;

            AdicionarMovimentacao(TipoMovimentacao.Credito, valor, descricao, saldoAnterior);
        }

        public void Debitar(decimal valor, string descricao = "")
        {
            ValidarOperacao(valor, descricao);

            if (Saldo < valor)
                throw new InvalidOperationException("Saldo insuficiente para realizar o débito");

            var saldoAnterior = Saldo;
            Saldo -= valor;
            DataUltimaMovimentacao = DateTime.UtcNow;

            AdicionarMovimentacao(TipoMovimentacao.Debito, valor, descricao, saldoAnterior);
        }

        public bool PodeSacar(decimal valor) => Ativa && Saldo >= valor;

        public void Inativar()
        {
            Ativa = false;
            DataUltimaMovimentacao = DateTime.UtcNow;
        }

        public void Ativar()
        {
            Ativa = true;
            DataUltimaMovimentacao = DateTime.UtcNow;
        }

        public decimal ObterSaldoAnteriorA(DateTime data)
        {
            var movimentacoesAnteriores = _movimentacoes
                .Where(m => m.DataMovimentacao < data)
                .OrderBy(m => m.DataMovimentacao);

            if (!movimentacoesAnteriores.Any())
                return 0;

            return movimentacoesAnteriores.Last().SaldoPosterior;
        }

        public IEnumerable<Movimentacao> ObterMovimentacoesPeriodo(DateTime inicio, DateTime fim)
        {
            return _movimentacoes
                .Where(m => m.DataMovimentacao >= inicio && m.DataMovimentacao <= fim)
                .OrderBy(m => m.DataMovimentacao);
        }

        private void ValidarOperacao(decimal valor, string descricao)
        {
            if (valor <= 0)
                throw new ArgumentException("Valor deve ser maior que zero", nameof(valor));

            if (!Ativa)
                throw new InvalidOperationException("Não é possível realizar operações em conta inativa");

            if (string.IsNullOrWhiteSpace(descricao))
                throw new ArgumentException("Descrição da operação é obrigatória", nameof(descricao));
        }

        private void AdicionarMovimentacao(TipoMovimentacao tipo, decimal valor, string descricao, decimal saldoAnterior)
        {
            var movimentacao = new Movimentacao
            {
                Id = Guid.NewGuid().ToString(),
                ContaId = Id,
                Tipo = tipo,
                Valor = valor,
                Descricao = descricao,
                DataMovimentacao = DataUltimaMovimentacao!.Value,
                SaldoAnterior = saldoAnterior,
                SaldoPosterior = Saldo
            };

            _movimentacoes.Add(movimentacao);
        }
    }

}
