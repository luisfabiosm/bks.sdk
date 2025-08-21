namespace Domain.Core.Models
{
    public record LimiteInfo
    {
        public string NumeroConta { get; init; } = string.Empty;
        public decimal LimiteDebitoDiario { get; init; }
        public decimal LimiteDebitoMensal { get; init; }
        public decimal LimiteTransferenciaDiaria { get; init; }
        public decimal UtilizadoDebitoDiario { get; init; }
        public decimal UtilizadoDebitoMensal { get; init; }
        public decimal UtilizadoTransferenciaDiaria { get; init; }
        public DateTime UltimaAtualizacao { get; init; }
        public DateTime DataResetDiario { get; init; }
        public DateTime DataResetMensal { get; init; }

        public decimal DisponibilidadeDebitoDiario => LimiteDebitoDiario - UtilizadoDebitoDiario;
        public decimal DisponibilidadeDebitoMensal => LimiteDebitoMensal - UtilizadoDebitoMensal;
        public decimal DisponibilidadeTransferenciaDiaria => LimiteTransferenciaDiaria - UtilizadoTransferenciaDiaria;
    }
}
