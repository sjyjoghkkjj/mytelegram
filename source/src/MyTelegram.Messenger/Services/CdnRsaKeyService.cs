using System.Security.Cryptography;

namespace MyTelegram.Messenger.Services;

public interface ICdnRsaKeyService : ISingletonDependency
{
    // Returns PEM public key for CDN DC
    string GetPublicKeyPem(int cdnDcId);
    // Sign data with CDN DC private key (used on CDN DC)
    ReadOnlyMemory<byte> Sign(int cdnDcId, ReadOnlyMemory<byte> data);
    // Verify data with CDN DC public key (used on master DC)
    bool Verify(int cdnDcId, ReadOnlyMemory<byte> data, ReadOnlyMemory<byte> signature);
}

public class CdnRsaKeyService(IOptionsMonitor<MyTelegramMessengerServerOptions> options, ILogger<CdnRsaKeyService> logger) : ICdnRsaKeyService
{
    private readonly ConcurrentDictionary<int, RSA> _rsaByDc = new();

    private RSA GetOrCreate(int dcId)
    {
        return _rsaByDc.GetOrAdd(dcId, id =>
        {
            // Generate ephemeral RSA key for dev; in prod, load from secure store
            var rsa = RSA.Create(2048);
            return rsa;
        });
    }

    public string GetPublicKeyPem(int cdnDcId)
    {
        var rsa = GetOrCreate(cdnDcId);
        var pub = rsa.ExportRSAPublicKeyPem();
        return pub;
    }

    public ReadOnlyMemory<byte> Sign(int cdnDcId, ReadOnlyMemory<byte> data)
    {
        var rsa = GetOrCreate(cdnDcId);
        var sig = rsa.SignData(data.ToArray(), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return sig;
    }

    public bool Verify(int cdnDcId, ReadOnlyMemory<byte> data, ReadOnlyMemory<byte> signature)
    {
        var rsa = GetOrCreate(cdnDcId);
        return rsa.VerifyData(data.ToArray(), signature.ToArray(), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }
}

