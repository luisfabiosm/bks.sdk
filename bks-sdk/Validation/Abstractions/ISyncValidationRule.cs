using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Validation.Abstractions;


public interface ISyncValidationRule<T>
{
    string RuleName { get; }
    string ErrorMessage { get; }
    bool IsValid(T instance);
}
