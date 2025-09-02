namespace Domain.Core.Entities
{
    public class AuditEntry
    {
        public string TransactionId { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TimeSpan? Duration { get; set; }
        public string? Error { get; set; }
        public Dictionary<string, object> Details { get; set; } = new();
    }

}
