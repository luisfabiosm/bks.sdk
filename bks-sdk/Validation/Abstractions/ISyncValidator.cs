

using bks.sdk.Common.Results;

namespace bks.sdk.Validation.Abstractions
{
    public interface ISyncValidator<T>
    {
        ValidationResult Validate(T instance);
    }
}
