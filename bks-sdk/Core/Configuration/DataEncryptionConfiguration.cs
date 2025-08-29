using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Configuration;

public record DataEncryptionConfiguration
{
    public bool Enabled { get; set; } = true;
    public string Algorithm { get; set; } = "AES256";
    public string? KeyVaultUrl { get; set; }
}
