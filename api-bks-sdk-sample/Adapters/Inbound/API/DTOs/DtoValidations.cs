using Adapters.Inbound.API.DTOs.Request;

namespace Adapters.Inbound.API.DTOs
{
    public static class DtoValidations
    {
        public static bool IsValid(this CreditoRequestDto dto, out List<string> errors)
        {
            errors = new List<string>();

            if (dto.NumeroConta==0)
                errors.Add("Número da conta de crédito é obrigatório");

            if (dto.Valor <= 0)
                errors.Add("Valor deve ser maior que zero");

            if (string.IsNullOrWhiteSpace(dto.Descricao))
                errors.Add("Descrição é obrigatória");

            return errors.Count == 0;
        }

        public static bool IsValid(this DebitoRequestDto dto, out List<string> errors)
        {
            errors = new List<string>();

            if (dto.NumeroConta==0)
                errors.Add("Número da conta de débito é obrigatório");

            if (dto.Valor <= 0)
                errors.Add("Valor deve ser maior que zero");

            if (string.IsNullOrWhiteSpace(dto.Descricao))
                errors.Add("Descrição é obrigatória");

            return errors.Count == 0;
        }

        public static bool IsValid(this CriarContaRequestDto dto, out List<string> errors)
        {
            errors = new List<string>();

            if (dto.Numero==0)
                errors.Add("Número da conta é obrigatório");

            if (string.IsNullOrWhiteSpace(dto.Titular))
                errors.Add("Nome do titular é obrigatório");

            if (dto.SaldoInicial < 0)
                errors.Add("Saldo inicial não pode ser negativo");

            return errors.Count == 0;
        }
    }
}
