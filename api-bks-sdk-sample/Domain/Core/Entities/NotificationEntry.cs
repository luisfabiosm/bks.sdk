namespace Domain.Core.Entities
{
    public class NotificationEntry
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Recipient { get; set; }
        public int? AccountNumber { get; set; }
        public string? TransactionType { get; set; }
        public DateTime Timestamp { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = "NORMAL";
    }


}
