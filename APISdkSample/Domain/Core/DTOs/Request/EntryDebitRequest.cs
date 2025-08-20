namespace Domain.Core.DTOs.Request
{
    public record EntryDebitRequest
    (
        int Branch,
        string AccountNumber,
        decimal Value,
        string Detail,
        string? Ref = null
    );
}
