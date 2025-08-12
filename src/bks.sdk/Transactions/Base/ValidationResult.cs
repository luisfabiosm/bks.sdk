using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Transactions.Base
{

    public readonly record struct ValidationResult
    {
        public bool IsValid { get; }
        public IReadOnlyList<string> Errors { get; }

        private ValidationResult(bool isValid, IReadOnlyList<string> errors)
        {
            IsValid = isValid;
            Errors = errors;
        }

        public static ValidationResult Valid() => new(true, Array.Empty<string>());

        public static ValidationResult Invalid(IEnumerable<string> errors) =>
            new(false, errors.ToList().AsReadOnly());
    }

}
