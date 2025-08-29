using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Security.Encryption;


public interface IDataEncryptor
{
    string Encrypt(string plainText);
    string Decrypt(string encryptedText);
    byte[] Encrypt(byte[] data);
    byte[] Decrypt(byte[] encryptedData);
    string EncryptSensitiveData(string data);
    string DecryptSensitiveData(string encryptedData);
}
