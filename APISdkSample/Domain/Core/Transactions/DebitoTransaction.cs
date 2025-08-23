using bks.sdk.Common.Results;
using bks.sdk.Transactions;

namespace Domain.Core.Transactions
{
    public record DebitoTransaction : BaseTransaction
    {
        public string NumeroConta { get; init; } = string.Empty;
        public decimal Valor { get; init; }
        public string Descricao { get; init; } = string.Empty;
        public string? Referencia { get; init; }
        public string? TipoDebito { get; init; } = "OPERACIONAL"; // OPERACIONAL, TAXA, AJUSTE

        public override ValidationResult ValidateTransaction()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(NumeroConta))
                errors.Add("Número da conta é obrigatório");

            if (Valor <= 0)
                errors.Add("Valor deve ser maior que zero");

            if (Valor > 100000) // Limite de R$ 100.000 para débitos
                errors.Add("Valor excede o limite máximo permitido de R$ 100.000");

            if (string.IsNullOrWhiteSpace(Descricao))
                errors.Add("Descrição é obrigatória");

            if (Descricao.Length > 200)
                errors.Add("Descrição deve ter no máximo 200 caracteres");

            // Validação específica de formato da conta
            if (!string.IsNullOrWhiteSpace(NumeroConta) &&
                !System.Text.RegularExpressions.Regex.IsMatch(NumeroConta, @"^\d{5}-\d$"))
                errors.Add("Formato de conta inválido (esperado: 12345-6)");

            return errors.Any()
                ? ValidationResult.Failure(errors)
                : ValidationResult.Success();
        }
    }

}
