using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using SMTSP.Extensions;

namespace SMTSP.Encryption;

internal class SessionEncryption
{
    private readonly AsymmetricCipherKeyPair _keyPair;

    public SessionEncryption()
    {
        _keyPair = EllipticCurveDiffieHellman.GenerateKeyPair();
    }

    public byte[] GetMyPublicKey()
    {
        return EllipticCurveDiffieHellman.ExportPublicKey(_keyPair.Public);
    }

    public byte[] CalculateAesKey(byte[] foreignPublicKeyBytes)
    {
        ECPublicKeyParameters foreignPublicKey = EllipticCurveDiffieHellman.LoadPublicKey(foreignPublicKeyBytes);
        return EllipticCurveDiffieHellman.GenerateAesKey(foreignPublicKey, _keyPair.Private);
    }

    public static byte[] GenerateIvBytes()
    {
        using var aes = Aes.Create();
        return aes.IV;
    }

    public static async Task EncryptStream(Stream inputStream, Stream dataToEncrypt, byte[] aesKey, byte[] iv, IProgress<long>? progress, CancellationToken cancellationToken)
    {
        using var aes = Aes.Create();
        await using var encryptedStream = new CryptoStream(inputStream, aes.CreateEncryptor(aesKey, iv), CryptoStreamMode.Write);
        await using StreamWriter encryptWriter = new(encryptedStream);
        await dataToEncrypt.CopyToAsyncWithProgress(encryptWriter.BaseStream, progress, cancellationToken);

        encryptedStream.Close();
    }

    public static Stream CreateDecryptedStream(Stream inputStream, byte[] aesKey, byte[] iv)
    {
        using var aliceAes = Aes.Create();
        var decryptedStream = new CryptoStream(inputStream, aliceAes.CreateDecryptor(aesKey, iv), CryptoStreamMode.Read);

        return decryptedStream;
    }
}