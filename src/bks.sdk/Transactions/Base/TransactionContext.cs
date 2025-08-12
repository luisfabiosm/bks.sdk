using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Transactions.Base
{
    public record TransactionContext
    {

        public required string ApplicationId { get; init; }

 
        public required string ApplicationName { get; init; }


        public string? UserId { get; init; }


        public IEnumerable<Claim> UserClaims { get; init; } = Array.Empty<Claim>();


        public string? IpAddress { get; init; }


        public string? UserAgent { get; init; }

 
        public Dictionary<string, string> Headers { get; init; } = new();


        public string Environment { get; init; } = "Production";


        public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;


        public HashSet<string> Permissions { get; init; } = new();


        public string? SessionId { get; init; }


        public Dictionary<string, object> CustomData { get; init; } = new();


        public bool HasPermission(string permission)
        {
            return Permissions.Contains(permission);
        }


        public bool HasAllPermissions(IEnumerable<string> permissions)
        {
            return permissions.All(p => Permissions.Contains(p));
        }


        public TransactionContext WithCustomData(string key, object value)
        {
            var newCustomData = new Dictionary<string, object>(CustomData) { [key] = value };
            return this with { CustomData = newCustomData };
        }
    }


}
