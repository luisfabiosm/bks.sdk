using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Configuration;


public record SecurityConfiguration
{
    public JwtConfiguration Jwt { get; set; } = new();
    public DataEncryptionConfiguration DataEncryption { get; set; } = new();
}
