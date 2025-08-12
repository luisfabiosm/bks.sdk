using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Authentication
{
    public readonly record struct ApplicationValidationResult
    {
        public bool IsValid { get; }
        public string? ErrorMessage { get; }
        public ApplicationInfo? ApplicationInfo { get; }

        private ApplicationValidationResult(bool isValid, string? errorMessage = null, ApplicationInfo? applicationInfo = null)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
            ApplicationInfo = applicationInfo;
        }

        public static ApplicationValidationResult Valid(ApplicationInfo applicationInfo)
            => new(true, applicationInfo: applicationInfo);

        public static ApplicationValidationResult Invalid(string errorMessage)
            => new(false, errorMessage);
    }
}
