using System.Security.Cryptography;

namespace SMTSP;

internal class EncryptionService
{
    private static EncryptionService? _instance;
    public static EncryptionService Instance => _instance ??= new EncryptionService();

    public static RSA CreateKeyPair()
    {
        RSA rsa = RSA.Create();
        // return rsa.ExportParameters();

        return rsa;
    }

    public static byte[]? Decrypt(byte[] content, RSAParameters rsaKeyInfo, bool DoOAEPPadding)
    {
        try
        {
            using RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(rsaKeyInfo);


            byte[] decryptedData = rsa.Decrypt(content, DoOAEPPadding);
            return decryptedData;
        }
        catch (CryptographicException e)
        {
            Console.WriteLine(e.ToString());

            return null;
        }
    }

    public static byte[]? Encrypt(byte[] content, RSAParameters rsaKeyInfo, bool DoOAEPPadding)
    {
        try
        {
            using RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(rsaKeyInfo);

            byte[] encryptedData = rsa.Encrypt(content, DoOAEPPadding);
            return encryptedData;
        }
        catch (CryptographicException e)
        {
            Console.WriteLine(e.Message);

            return null;
        }
    }
}