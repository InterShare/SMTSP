using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SMTSP;

public static class EncryptionHelper
{
    public static X509Certificate2 GenerateSelfSignedCertificate()
    {
        var ecdsa = ECDsa.Create();
        var machineName = Dns.GetHostName();
        var certificateRequest = new CertificateRequest($"CN={machineName}", ecdsa, HashAlgorithmName.SHA256);

        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddIpAddress(IPAddress.Loopback);
        sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
        sanBuilder.AddDnsName(machineName);
        sanBuilder.AddDnsName(Environment.MachineName);

        certificateRequest.CertificateExtensions.Add(sanBuilder.Build());
        var certificate = certificateRequest.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(10));

        // It seems like we have to provide a password, or else the private key won't be exported.
        const string exportPassword = "notnull";
        return new X509Certificate2(certificate.Export(X509ContentType.Pfx, exportPassword), exportPassword);
    }
}
