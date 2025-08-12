using System;
using System.Threading;
using System.Threading.Tasks;


namespace bks.sdk.Core.Authentication
{
    public interface IBksAuthenticationService
    {
 
        ValueTask<AuthenticationResult> AuthenticateAsync(string applicationKey, CancellationToken cancellationToken = default);

    
        ValueTask<bool> HasPermissionAsync(string applicationKey, string permission, CancellationToken cancellationToken = default);

 
        AuthenticationContext? GetCurrentContext();
    }

}
