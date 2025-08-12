using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Cryptography
{
    [Flags]
    public enum StringGenerationOptions
    {
        None = 0,
        Uppercase = 1,
        Lowercase = 2,
        Digits = 4,
        Special = 8,
        UrlSafe = 16,
        Alphanumeric = Uppercase | Lowercase | Digits,
        All = Uppercase | Lowercase | Digits | Special
    }

}
