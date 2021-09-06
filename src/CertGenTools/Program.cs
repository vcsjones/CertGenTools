using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

GenerateType generateType = Prompts.GenerateTypePrompt();
CertificateProfile profile = Prompts.CertProfilePrompt();
KeyAlgorithm algorithm = Prompts.KeyTypePrompt();
int days = generateType == GenerateType.SelfSignedCertificate ? Prompts.ValidityDays() : -1;
ExportType exportType = Prompts.ExportTypePrompt(generateType);
PasswordResult password = Prompts.PasswordPrompt("Would you like to password protect your key?");
string saveTo = Prompts.PathPrompt();

AsymmetricAlgorithm privateKey = algorithm switch {
    EcdsaKey ecdsa => ECDsa.Create(ecdsa.Curve),
    RsaKey rsa => RSA.Create(rsa.KeySize),
    _ => throw new InvalidOperationException()
};

// These PBE parameters suck as defaults but unfortunately many platforms lack support for AES
// and modern encryption. We favor compat, for now.
PbeParameters defaultPbeParamters = new(
    PbeEncryptionAlgorithm.TripleDes3KeyPkcs12,
    HashAlgorithmName.SHA1,
    600_000);

CertificateRequest request = algorithm switch {
    RsaKey rsa => new CertificateRequest($"CN={profile.CommonName}", (RSA)privateKey, rsa.HashAlgorithm, RSASignaturePadding.Pkcs1),
    EcdsaKey ecdsa => new CertificateRequest($"CN={profile.CommonName}", (ECDsa)privateKey, ecdsa.HashAlgorithm),
    _ => throw new InvalidOperationException()
};

if (profile.DnsNames.Length > 0)
{
    SubjectAlternativeNameBuilder builder = new();
    
    foreach(var name in profile.DnsNames)
    {
        builder.AddDnsName(name);
    }

    request.CertificateExtensions.Add(builder.Build(critical: false));
}

if (profile.Ekus.Count > 0)
{
    X509EnhancedKeyUsageExtension ekuExt = new(profile.Ekus, critical: false);
    request.CertificateExtensions.Add(ekuExt);
}

X509KeyUsageFlags keyUsageFlags = X509KeyUsageFlags.DigitalSignature;

if (algorithm is RsaKey)
{
    keyUsageFlags |= X509KeyUsageFlags.KeyAgreement;
}

if (generateType == GenerateType.SelfSignedCertificate)
{
    keyUsageFlags |= X509KeyUsageFlags.KeyCertSign;
    request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
}

request.CertificateExtensions.Add(new X509KeyUsageExtension(keyUsageFlags, critical: false));



if (generateType == GenerateType.CertificateRequest)
{
    byte[] requestDer = request.CreateSigningRequest();
    (bool encrypted, byte[] pkc8) = ExportPrivateKeyPkcs8();

    using var sr = new StreamWriter(saveTo, false);
    sr.Write(PemEncoding.Write("CERTIFICATE REQUEST", requestDer));
    sr.Write("\n");
    sr.Write(PemEncoding.Write(encrypted ? "ENCRYPTED PRIVATE KEY" : "PRIVATE KEY", pkc8));
    sr.Write("\n");
}
else if (generateType == GenerateType.SelfSignedCertificate)
{
    DateTimeOffset notBefore = DateTimeOffset.UtcNow;
    DateTimeOffset notAfter = notBefore.AddDays(days);
    using X509Certificate2 cert = request.CreateSelfSigned(notBefore, notAfter);

    if (exportType == ExportType.Pem)
    {
        (bool encrypted, byte[] pkc8) = ExportPrivateKeyPkcs8();
        using var sr = new StreamWriter(saveTo, false);
        sr.Write(PemEncoding.Write("CERTIFICATE", cert.RawData));
        sr.Write("\n");
        sr.Write(PemEncoding.Write(encrypted ? "ENCRYPTED PRIVATE KEY" : "PRIVATE KEY", pkc8));
        sr.Write("\n");
    }
    else if (exportType == ExportType.Pkcs12)
    {
        Pkcs12SafeContents certContents = new();
        certContents.AddCertificate(cert);
        Pkcs12SafeContents keyContents = new();

        Pkcs12Builder builder = new();
        builder.AddSafeContentsUnencrypted(certContents);
        
        switch (password)
        {
            case NoPasswordResult:
                keyContents.AddKeyUnencrypted(privateKey);
                builder.AddSafeContentsUnencrypted(keyContents);
                builder.SealWithoutIntegrity();
                break;
            case SecretPasswordResult secret:
                keyContents.AddShroudedKey(privateKey, secret.Password, defaultPbeParamters);
                builder.AddSafeContentsEncrypted(keyContents, secret.Password, defaultPbeParamters);
                builder.SealWithMac(secret.Password, defaultPbeParamters.HashAlgorithm, defaultPbeParamters.IterationCount);
                break;
        }

        using var sr = new FileStream(saveTo, FileMode.Create, FileAccess.Write);
        sr.Write(builder.Encode());
    }
}
else
{
    throw new InvalidOperationException("Unknown type");
}

(bool encrypted, byte[] pkcs8) ExportPrivateKeyPkcs8() => password switch {
    NoPasswordResult => (false, privateKey.ExportPkcs8PrivateKey()),
    SecretPasswordResult secret => (true, privateKey.ExportEncryptedPkcs8PrivateKey(secret.Password, defaultPbeParamters)),
    _ => throw new InvalidOperationException(),
};