using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Security.Models;


public class SecurityContext
{
    public ClaimsPrincipal? User { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime RequestStartedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();

    public string? UserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    public string? UserName => User?.FindFirst(ClaimTypes.Name)?.Value;
    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool HasRole(string role) => User?.IsInRole(role) ?? false;
    public bool HasPermission(string permission) => User?.HasClaim("permission", permission) ?? false;
}