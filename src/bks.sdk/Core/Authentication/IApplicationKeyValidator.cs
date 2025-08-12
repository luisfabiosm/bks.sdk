using System;
using System.Threading;
using System.Threading.Tasks;


namespace bks.sdk.Core.Authentication
{
    public interface IApplicationKeyValidator
    {

        ValueTask<ApplicationValidationResult> ValidateAsync(string applicationKey, CancellationToken cancellationToken = default);

        ValueTask<ApplicationInfo?> GetApplicationInfoAsync(string applicationKey, CancellationToken cancellationToken = default);
    }
}
