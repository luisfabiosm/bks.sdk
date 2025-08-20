namespace Domain.Core.Models.Results
{
    public record AnaliseDeTransferenciaResult
    {
        public bool Aprovado { get; set; }
        public string Motivo { get; set; }
    }
}
