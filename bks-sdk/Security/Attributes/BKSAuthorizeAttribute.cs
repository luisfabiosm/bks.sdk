using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Security.Attributes;
public class BKSAuthorizeAttribute : AuthorizeAttribute
{
    public BKSAuthorizeAttribute(string role, string? permission = null)
    {
        Policy = $"BKSPolicy_{role}_{permission ?? ""}";
    }
}

