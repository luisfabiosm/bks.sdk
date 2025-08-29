using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Middlewares.Security;

public class SecurityHeadersOptions
{
    public bool AddXContentTypeOptions { get; set; } = true;
    public bool AddXFrameOptions { get; set; } = true;
    public string XFrameOptionsValue { get; set; } = "DENY";
    public bool AddXXSSProtection { get; set; } = true;
    public bool AddHSTS { get; set; } = false; // Deve ser habilitado apenas em HTTPS
    public string HSTSValue { get; set; } = "max-age=31536000; includeSubDomains";
    public bool AddCSP { get; set; } = false; // Deve ser configurado conforme necessário
    public string CSPValue { get; set; } = "default-src 'self'";
    public bool AddReferrerPolicy { get; set; } = true;
    public string ReferrerPolicyValue { get; set; } = "strict-origin-when-cross-origin";
    public bool AddPermissionsPolicy { get; set; } = false;
    public string PermissionsPolicyValue { get; set; } = "geolocation=(), microphone=(), camera=()";
}
