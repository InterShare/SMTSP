using System.Security.Cryptography;
using SMTSP.Extensions;

namespace SMTSP.Encryption;

internal class SessionEncryption
{
    private readonly ECDiffieHellman _diffieHellmanInstance;

    public SessionEncryption()
    {
        _diffieHellmanInstance = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP521);
    }

    public byte[] GetMyPublicKey()
    {
        return _diffieHellmanInstance.ExportSubjectPublicKeyInfo();
    }

    public byte[] CalculateAesKey(byte[] foreignPublicKeyBytes)
    {
        // TODO: Find better method to import public key.
        var foreignDh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP521);
        foreignDh.ImportSubjectPublicKeyInfo(foreignPublicKeyBytes, out int _);

        return _diffieHellmanInstance.DeriveKeyMaterial(foreignDh.PublicKey);
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