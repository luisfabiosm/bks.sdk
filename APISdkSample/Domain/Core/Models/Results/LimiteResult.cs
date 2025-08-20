namespace Domain.Core.Models.Results
{
    public record LimiteResult
    {
        public bool Aprovado { get; set; }
        public string Motivo { get; set; }

        // Construtor para resultados de sucesso
        public LimiteResult()
        {
            Aprovado = true;
        }

        // Construtor para resultados de falha
        public LimiteResult(string motivo)
        {
            Aprovado = false;
            Motivo = motivo;
        }
    }
}
