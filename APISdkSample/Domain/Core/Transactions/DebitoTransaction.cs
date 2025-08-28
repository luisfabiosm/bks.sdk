using bks.sdk.Common.Results;
using bks.sdk.Transactions;
using Domain.Core.Models.DTOs.Request;

namespace Domain.Core.Transactions
{
    public sealed record DebitoTransaction : BaseTransaction
    {
        public int Agencia { get; init; } = 1;
        public int NumeroConta { get; init; } = 0;
        public decimal Valor { get; init; }
        public string Descricao { get; init; } = string.Empty;
        public string? Referencia { get; init; }

        // Construtor privado para forçar uso do factory
        private DebitoTransaction() { }

        // Factory method principal
        public static DebitoTransaction Create(
            int numeroConta,
            decimal valor,
            string descricao,
            int agencia = 1,
            string? referencia = null)
        {
            var transaction = new DebitoTransaction
            {
                Agencia = agencia,
                NumeroConta = numeroConta,
                Valor = valor,
                Descricao = descricao ?? string.Empty,
                Referencia = referencia
            };

            return transaction;
        }

        public static DebitoTransaction Create(DebitoRequest request)
        {
            return Create(
                request.NumeroConta,
                request.Valor,
                request.Descricao,
                request.Agencia,
                request.Referencia);
        }


        public override ValidationResult ValidateTransaction()
        {
            var errors = new List<string>();

            if (NumeroConta == 0)
                errors.Add("Número da conta é obrigatório");

            if (Valor <= 0)
                errors.Add("Valor deve ser maior que zero");

            if (Valor > 100000) // Limite de R$ 100.000 para débitos
                errors.Add("Valor excede o limite máximo permitido de R$ 100.000");

            if (string.IsNullOrWhiteSpace(Descricao))
                errors.Add("Descrição é obrigatória");

            if (Descricao.Length > 200)
                errors.Add("Descrição deve ter no máximo 200 caracteres");

            return errors.Any()
                ? ValidationResult.Failure(errors)
                : ValidationResult.Success();
        }
    }


}
