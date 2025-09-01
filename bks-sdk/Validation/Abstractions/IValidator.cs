

using bks.sdk.Common.Results;

namespace bks.sdk.Validation.Abstractions;

public interface IValidator<T>
{
    Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);
    ValidationResult Validate(T instance);
}