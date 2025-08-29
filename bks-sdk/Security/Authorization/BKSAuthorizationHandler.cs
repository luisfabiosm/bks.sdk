using bks.sdk.Observability.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Security.Authorization;

public class BKSAuthorizationHandler : AuthorizationHandler<BKSAuthorizationRequirement>
{
    private readonly IBKSLogger _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BKSAuthorizationHandler(IBKSLogger logger, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        BKSAuthorizationRequirement requirement)
    {
        var user = context.User;
        var httpContext = _httpContextAccessor.HttpContext;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            _logger.Warn("Usuário não autenticado tentando acessar recurso protegido");
            context.Fail();
            return Task.CompletedTask;
        }

        // Verificar role obrigatória
        if (!user.IsInRole(requirement.RequiredRole))
        {
            _logger.Warn($"Usuário {user.Identity?.Name} não possui a role necessária: {requirement.RequiredRole}");
            context.Fail();
            return Task.CompletedTask;
        }

        // Verificar permissão específica se fornecida
        if (!string.IsNullOrWhiteSpace(requirement.RequiredPermission))
        {
            var hasPermission = user.HasClaim("permission", requirement.RequiredPermission);
            if (!hasPermission)
            {
                _logger.Warn($"Usuário {user.Identity?.Name} não possui a permissão necessária: {requirement.RequiredPermission}");
                context.Fail();
                return Task.CompletedTask;
            }
        }

        _logger.Trace($"Usuário {user.Identity?.Name} autorizado para role: {requirement.RequiredRole}");
        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}


