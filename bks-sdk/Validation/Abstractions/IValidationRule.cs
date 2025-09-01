using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Validation.Abstractions;


public interface IValidationRule<T>
{
    string RuleName { get; }
    string ErrorMessage { get; }
    Task<bool> IsValidAsync(T instance, CancellationToken cancellationToken = default);
    bool IsValid(T instance);
}
