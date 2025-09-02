using bks.sdk.Processing.Mediator.Abstractions;

namespace Domain.Core.Commands
{
    public record ProcessarCreditoCommand : BaseRequest<ProcessarCreditoResponse>
    {
        public int NumeroContaCredito { get; init; } 
        public decimal Valor { get; init; }
        public string Descricao { get; init; } = string.Empty;
        public string? Referencia { get; init; }
        public string? Observacoes { get; init; }
        public bool NotificarCliente { get; init; } = true;

        // Propriedades de validação
        public bool IsValido
        {
            get
            {
                return !(NumeroContaCredito ==0) &&
                       Valor > 0 &&
                       Valor <= 1_000_000 &&
                       !string.IsNullOrWhiteSpace(Descricao) &&
                       Descricao.Length <= 200;
            }
        }

        public List<string> ObterErrosValidacao()
        {
            var erros = new List<string>();

            if (NumeroContaCredito==0)
                erros.Add("Número da conta de crédito é obrigatório");

            if (Valor <= 0)
                erros.Add("Valor deve ser maior que zero");

            if (Valor > 1_000_000)
                erros.Add("Valor não pode exceder R$ 1.000.000,00");

            if (string.IsNullOrWhiteSpace(Descricao))
                erros.Add("Descrição é obrigatória");

            else if (Descricao.Length > 200)
                erros.Add("Descrição deve ter no máximo 200 caracteres");

            return erros;
        }
    }

}
