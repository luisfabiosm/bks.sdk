using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Authentication
{
    public record ApplicationInfo
    {
        public required string ApplicationId { get; init; }
        public required string ApplicationName { get; init; }
        public required string ApplicationKey { get; init; }
        public required ApplicationType Type { get; init; }
        public required ApplicationStatus Status { get; init; }
        public required DateTime CreatedAt { get; init; }
        public DateTime? LastAccess { get; init; }
        public HashSet<string> Permissions { get; init; } = new();
        public Dictionary<string, object> Metadata { get; init; } = new();
    }
}
