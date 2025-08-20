namespace Domain.Core.Models.Results
{
    public record ReservaResult
    {
        public bool Success { get; set; }
        public string Mensagem { get; set; }
    }
}
