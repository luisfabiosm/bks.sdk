using bks.sdk.Common.Results;


namespace bks.sdk.Validation.Abstractions
{
    public interface IAsyncValidator<T>
    {
        Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);
    }

}
