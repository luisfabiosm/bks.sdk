namespace Domain.Core.Transactions
{
    public static class TransactionFactory
    {
        public static CreditoTransaction CriarCreditoTransaction(
            string numeroConta,
            decimal valor,
            string descricao,
            string? referencia = null)
        {
            var transaction = new CreditoTransaction
            {
                NumeroContaCredito = numeroConta?.Trim() ?? string.Empty,
                Valor = valor,
                Descricao = descricao?.Trim() ?? string.Empty,
                Referencia = referencia?.Trim()
            };

            ValidarCreditoTransaction(transaction);
            return transaction;
        }

        public static DebitoTransaction CriarDebitoTransaction(
            int numeroConta,
            decimal valor,
            string descricao,
            string? referencia = null,
            decimal? limiteOperacao = null)
        {
            var transaction = new DebitoTransaction
            {
                NumeroConta = numeroConta,
                Valor = valor,
                Descricao = descricao?.Trim() ?? string.Empty,
                Referencia = referencia?.Trim(),
                LimiteOperacao = limiteOperacao
            };

            ValidarDebitoTransaction(transaction);
            return transaction;
        }

        private static void ValidarCreditoTransaction(CreditoTransaction transaction)
        {
            var erros = new List<string>();

            if (!transaction.IsValorValido)
                erros.Add($"Valor inválido: deve ser maior que zero e menor que R$ 1.000.000,00. Valor informado: {transaction.Valor:C}");

            if (!transaction.IsContaValida)
                erros.Add("Número da conta de crédito é obrigatório");

            if (!transaction.IsDescricaoValida)
                erros.Add("Descrição é obrigatória e deve ter no máximo 200 caracteres");

            if (erros.Count > 0)
                throw new ArgumentException($"Transação de crédito inválida: {string.Join("; ", erros)}");
        }

        private static void ValidarDebitoTransaction(DebitoTransaction transaction)
        {
            var erros = new List<string>();

            if (!transaction.IsValorValido)
                erros.Add($"Valor inválido: deve ser maior que zero e menor que o limite operacional. Valor informado: {transaction.Valor:C}");

            if (!transaction.IsContaValida)
                erros.Add("Número da conta de débito é obrigatório");

            if (!transaction.IsDescricaoValida)
                erros.Add("Descrição é obrigatória e deve ter no máximo 200 caracteres");

            if (erros.Count > 0)
                throw new ArgumentException($"Transação de débito inválida: {string.Join("; ", erros)}");
        }
    }
}
