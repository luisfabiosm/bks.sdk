namespace Domain.Core.ValueObjects
{
    public record Saldo
    {
        public decimal Valor { get; }

        public Saldo(decimal valor)
        {
            if (valor < 0)
                throw new ArgumentException("Valor monetário não pode ser negativo");

            Valor = Math.Round(valor, 2);
        }

        public static implicit operator decimal(Saldo dinheiro) => dinheiro.Valor;
        public static implicit operator Saldo(decimal valor) => new(valor);

        public Saldo Somar(Saldo outro) => new(Valor + outro.Valor);
        public Saldo Subtrair(Saldo outro) => new(Valor - outro.Valor);

        public override string ToString() => Valor.ToString("C");
    }
}
