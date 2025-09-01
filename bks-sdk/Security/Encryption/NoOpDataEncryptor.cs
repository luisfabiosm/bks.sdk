using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Security.Encryption;

public class NoOpDataEncryptor : IDataEncryptor
{
    public string Encrypt(string plainText) => plainText;
    public string Decrypt(string encryptedText) => encryptedText;
    public byte[] Encrypt(byte[] data) => data;
    public byte[] Decrypt(byte[] encryptedData) => encryptedData;
    public string EncryptSensitiveData(string data) => data;
    public string DecryptSensitiveData(string encryptedData) => encryptedData;
}