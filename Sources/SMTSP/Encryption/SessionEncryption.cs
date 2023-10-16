using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;

namespace SMTSP.Encryption;

internal class SessionEncryption
{
    private readonly AsymmetricCipherKeyPair _keyPair = EllipticCurveDiffieHellman.GenerateKeyPair();

    public const int PublicKeyLength = 67;

    public byte[] GetMyPublicKey()
    {
        return EllipticCurveDiffieHellman.ExportPublicKey(_keyPair.Public);
    }

    public byte[] CalculateAesKey(byte[] foreignPublicKeyBytes)
    {
        var foreignPublicKey = EllipticCurveDiffieHellman.LoadPublicKey(foreignPublicKeyBytes);
        return EllipticCurveDiffieHellman.GenerateAesKey(foreignPublicKey, _keyPair.Private);
    }

    public static byte[] GenerateIvBytes()
    {
        using var aes = Aes.Create();
        return aes.IV;
    }

    public static CryptoStream CreateCryptoStream(Stream inputStream, byte[] aesKey, byte[] iv)
    {
        using var aes = Aes.Create();
        var encryptedStream = new CryptoStream(inputStream, aes.CreateEncryptor(aesKey, iv), CryptoStreamMode.Write);

        return encryptedStream;
    }
}
