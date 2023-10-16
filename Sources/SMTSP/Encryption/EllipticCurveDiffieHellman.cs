using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace SMTSP.Encryption;

internal static class EllipticCurveDiffieHellman
{
    private static ECDomainParameters? _curveParameters;

    private static ECDomainParameters CurveParameters
    {
        get
        {
            if (_curveParameters == null)
            {
                var x9Ec = NistNamedCurves.GetByName("P-521");
                _curveParameters = new ECDomainParameters(x9Ec.Curve, x9Ec.G, x9Ec.N, x9Ec.H, x9Ec.GetSeed());
            }

            return _curveParameters;
        }
    }

    public static AsymmetricCipherKeyPair GenerateKeyPair()
    {
        var keyPairGenerator = (ECKeyPairGenerator) GeneratorUtilities.GetKeyPairGenerator("ECDH");
        keyPairGenerator.Init(new ECKeyGenerationParameters(CurveParameters, new SecureRandom()));

        return keyPairGenerator.GenerateKeyPair();
    }

    public static byte[] ExportPublicKey(AsymmetricKeyParameter keyPair)
    {
        return (keyPair as ECPublicKeyParameters)!.Q.GetEncoded(true);
    }

    public static byte[] ExportPrivateKey(AsymmetricKeyParameter keyPair)
    {
        return (keyPair as ECPrivateKeyParameters)!.D.ToByteArray();
    }

    public static ECPublicKeyParameters LoadPublicKey(byte[] data)
    {
        var pubKey = new ECPublicKeyParameters(CurveParameters.Curve.DecodePoint(data), CurveParameters);

        return pubKey;
    }

    public static AsymmetricKeyParameter LoadPrivateKey(byte[] data)
    {
        return new ECPrivateKeyParameters(new BigInteger(data), CurveParameters);
    }

    public static byte[] GenerateAesKey(ECPublicKeyParameters foreignPublicKey, AsymmetricKeyParameter privateKey)
    {
        var basicAgreement = AgreementUtilities.GetBasicAgreement("ECDH");
        basicAgreement.Init(privateKey);
        var sharedSecret = basicAgreement.CalculateAgreement(foreignPublicKey);
        var sharedSecretBytes = sharedSecret.ToByteArray();

        IDigest digest = new Sha256Digest();
        var symmetricKey = new byte[digest.GetDigestSize()];
        digest.BlockUpdate(sharedSecretBytes, 0, sharedSecretBytes.Length);
        digest.DoFinal(symmetricKey, 0);

        return symmetricKey;
    }
}
