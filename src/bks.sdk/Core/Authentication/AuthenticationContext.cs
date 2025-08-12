using bks.sdk.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Authentication
{
    public record AuthenticationContext
    {
        public required string ApplicationId { get; init; }
        public required string ApplicationName { get; init; }
        public required ApplicationType ApplicationType { get; init; }
        public required HashSet<string> Permissions { get; init; }
        public required DateTime AuthenticatedAt { get; init; }
        public required string SessionId { get; init; }
    }
}
