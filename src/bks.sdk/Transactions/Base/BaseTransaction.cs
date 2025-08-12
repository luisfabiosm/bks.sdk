
using bks.sdk.Core.Cryptography;
using bks.sdk.Core.Mediator;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Text.Json;


namespace bks.sdk.Transactions.Base
{
    public abstract record BaseTransaction<TResponse> : ITransaction<TResponse>
    {

        public string TransactionId { get; init; } = Guid.NewGuid().ToString("N");


        public string CorrelationId { get; init; } = Guid.NewGuid().ToString("N");


        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;


        public Dictionary<string, object> Metadata { get; init; } = new();


        public virtual int Version => 1;


        public virtual TimeSpan Timeout => TimeSpan.FromSeconds(30);

  
        public virtual bool RequiresAuthentication => true;


        public virtual IEnumerable<string> RequiredPermissions => Array.Empty<string>();


        public virtual string GenerateSecureToken(ISecureTokenGenerator tokenGenerator)
        {
            if (tokenGenerator == null)
                throw new ArgumentNullException(nameof(tokenGenerator));

            var tokenTask = tokenGenerator.GenerateTokenAsync(this, TimeSpan.FromHours(24));
            return tokenTask.GetAwaiter().GetResult().Token;
        }

 
        public virtual string ToJson()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            return JsonSerializer.Serialize(this, GetType(), options);
        }

        public virtual string ComputeHash()
        {
            var json = ToJson();
            return TokenHasher.ComputeSha256Hash(json, TransactionId);
        }


        public virtual ValidationResult Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(TransactionId))
                errors.Add("Transaction ID cannot be null or empty");

            if (string.IsNullOrWhiteSpace(CorrelationId))
                errors.Add("Correlation ID cannot be null or empty");

            if (CreatedAt == default)
                errors.Add("Created date must be set");

            // Validação específica da transação
            var specificValidation = ValidateSpecific();
            if (!specificValidation.IsValid)
                errors.AddRange(specificValidation.Errors);

            return errors.Count == 0
                ? ValidationResult.Valid()
                : ValidationResult.Invalid(errors);
        }

        protected virtual ValidationResult ValidateSpecific()
        {
            return ValidationResult.Valid();
        }


        public BaseTransaction<TResponse> WithMetadata(string key, object value)
        {
            var newMetadata = new Dictionary<string, object>(Metadata) { [key] = value };
            return this with { Metadata = newMetadata };
        }


        public BaseTransaction<TResponse> WithCorrelationId(string correlationId)
        {
            return this with { CorrelationId = correlationId };
        }

        public BaseTransaction<TResponse> Duplicate()
        {
            return this with
            {
                TransactionId = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTimeOffset.UtcNow
            };
        }
    }

}
