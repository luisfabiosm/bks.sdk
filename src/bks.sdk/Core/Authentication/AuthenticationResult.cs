using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Authentication
{
    public readonly record struct AuthenticationResult
    {
        public bool IsAuthenticated { get; }
        public ApplicationInfo? ApplicationInfo { get; }
        public string? ErrorMessage { get; }
        public AuthenticationContext? Context { get; }

        private AuthenticationResult(bool isAuthenticated, ApplicationInfo? applicationInfo = null,
            string? errorMessage = null, AuthenticationContext? context = null)
        {
            IsAuthenticated = isAuthenticated;
            ApplicationInfo = applicationInfo;
            ErrorMessage = errorMessage;
            Context = context;
        }

        public static AuthenticationResult Success(ApplicationInfo applicationInfo, AuthenticationContext context)
            => new(true, applicationInfo, context: context);

        public static AuthenticationResult Failed(string errorMessage)
            => new(false, errorMessage: errorMessage);
    }
}
