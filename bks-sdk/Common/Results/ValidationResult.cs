using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Common.Results
{
    public class ValidationResult
    {
        public bool IsValid { get; }
        public List<string> Errors { get; }

        private ValidationResult(bool isValid, List<string> errors)
        {
            IsValid = isValid;
            Errors = errors ?? new List<string>();
        }

        public static ValidationResult Success() => new(true, new List<string>());
        public static ValidationResult Failure(List<string> errors) => new(false, errors);
        public static ValidationResult Failure(string error) => new(false, new List<string> { error });
    }
}
