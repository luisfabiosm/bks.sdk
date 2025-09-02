namespace Domain.Core.ValueObjects
{
    public record ContaNumero
    {
        public string Valor { get; }

        public ContaNumero(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new ArgumentException("Número da conta não pode ser vazio");

            if (valor.Length < 5)
                throw new ArgumentException("Número da conta deve ter pelo menos 5 caracteres");

            Valor = valor.Trim();
        }

        public static implicit operator string(ContaNumero contaNumero) => contaNumero.Valor;
        public static implicit operator ContaNumero(string valor) => new(valor);
    }

}
