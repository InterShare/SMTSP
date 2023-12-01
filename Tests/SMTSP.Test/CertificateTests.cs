using System.Security.Cryptography.X509Certificates;
using NUnit.Framework;

namespace SMTSP.Test;

public class CertificateTests
{
    [Test]
    public void TestCertificatePersistence()
    {
        // Store the certificate
        var certificate = CertificateHelper.GenerateSelfSignedCertificate();
        var thumbprint = certificate.Thumbprint;

        Assert.IsNotEmpty(thumbprint);
        Assert.IsTrue(certificate.HasPrivateKey);

        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        store.Open(OpenFlags.MaxAllowed);

        store.Add(certificate);
        store.Close();

        // Get the certificate from storage
        var importedCertificate = RetrieveCertificate(thumbprint);

        Assert.That(importedCertificate.HasPrivateKey, Is.True);
        Assert.AreEqual(importedCertificate.Thumbprint, certificate.Thumbprint);
        Assert.AreEqual(importedCertificate.SubjectName.Name, certificate.SubjectName.Name);
        Assert.That(certificate.GetECDsaPrivateKey()?.ExportECPrivateKey(),
            Is.EqualTo(expected: importedCertificate.GetECDsaPrivateKey()?.ExportECPrivateKey()));
    }

    private static X509Certificate2 RetrieveCertificate(string thumbprint)
    {
        using var store = new X509Store(StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadOnly);
        var collection = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);

        Assert.That(collection, Is.Not.Empty);

        var certificate = collection[0];
        Assert.That(certificate, Is.Not.Null);

        return certificate;
    }
}
