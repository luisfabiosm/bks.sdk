using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Authentication
{
    internal sealed class BksAuthenticationService : IBksAuthenticationService
    {
        private readonly IApplicationKeyValidator _keyValidator;
        private readonly ThreadLocal<AuthenticationContext?> _currentContext;

        public BksAuthenticationService(IApplicationKeyValidator keyValidator)
        {
            _keyValidator = keyValidator ?? throw new ArgumentNullException(nameof(keyValidator));
            _currentContext = new ThreadLocal<AuthenticationContext?>();
        }

        public async ValueTask<AuthenticationResult> AuthenticateAsync(string applicationKey, CancellationToken cancellationToken = default)
        {
            var validationResult = await _keyValidator.ValidateAsync(applicationKey, cancellationToken);

            if (!validationResult.IsValid)
            {
                return AuthenticationResult.Failed(validationResult.ErrorMessage ?? "Authentication failed");
            }

            var context = new AuthenticationContext
            {
                ApplicationId = validationResult.ApplicationInfo!.ApplicationId,
                ApplicationName = validationResult.ApplicationInfo.ApplicationName,
                ApplicationType = validationResult.ApplicationInfo.Type,
                Permissions = validationResult.ApplicationInfo.Permissions,
                AuthenticatedAt = DateTime.UtcNow,
                SessionId = Guid.NewGuid().ToString("N")
            };

            _currentContext.Value = context;

            return AuthenticationResult.Success(validationResult.ApplicationInfo, context);
        }

        public async ValueTask<bool> HasPermissionAsync(string applicationKey, string permission, CancellationToken cancellationToken = default)
        {
            var appInfo = await _keyValidator.GetApplicationInfoAsync(applicationKey, cancellationToken);
            return appInfo?.Permissions.Contains(permission) ?? false;
        }

        public AuthenticationContext? GetCurrentContext()
        {
            return _currentContext.Value;
        }
    }


}
