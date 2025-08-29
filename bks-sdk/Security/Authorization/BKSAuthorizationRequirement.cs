using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Security.Authorization;

public class BKSAuthorizationRequirement : IAuthorizationRequirement
{
    public string RequiredRole { get; }
    public string? RequiredPermission { get; }

    public BKSAuthorizationRequirement(string requiredRole, string? requiredPermission = null)
    {
        RequiredRole = requiredRole;
        RequiredPermission = requiredPermission;
    }
}