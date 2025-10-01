using System.Security.Cryptography;

namespace MyTelegram.Messenger.Services;

public interface ICdnTokenService : ISingletonDependency
{
    (ReadOnlyMemory<byte> fileToken, ReadOnlyMemory<byte> encKey, ReadOnlyMemory<byte> encIv) GenerateRedirect(long fileId, int cdnDcId, TimeSpan? ttl = null);
    bool TryResolveFileId(ReadOnlyMemory<byte> fileToken, out long fileId);
    ReadOnlyMemory<byte> CreateRequestToken(ReadOnlyMemory<byte> fileToken);
    bool ValidateRequestToken(ReadOnlyMemory<byte> fileToken, ReadOnlyMemory<byte> requestToken);
}

public class CdnTokenService(IOptionsMonitor<MyTelegramMessengerServerOptions> options, ILogger<CdnTokenService> logger) : ICdnTokenService
{
    private readonly ConcurrentDictionary<string, (long FileId, int ExpireAt)> _map = new();

    public (ReadOnlyMemory<byte> fileToken, ReadOnlyMemory<byte> encKey, ReadOnlyMemory<byte> encIv) GenerateRedirect(long fileId, int cdnDcId, TimeSpan? ttl = null)
    {
        var token = RandomNumberGenerator.GetBytes(32);
        var key = Convert.ToBase64String(token);
        var now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var expireAt = now + (int)(ttl?.TotalSeconds ?? 3600);
        _map[key] = (fileId, expireAt);
        var encKey = RandomNumberGenerator.GetBytes(32); // placeholder, not used in demo
        var encIv = RandomNumberGenerator.GetBytes(16); // placeholder, not used in demo
        return (token, encKey, encIv);
    }

    public bool TryResolveFileId(ReadOnlyMemory<byte> fileToken, out long fileId)
    {
        var key = Convert.ToBase64String(fileToken.ToArray());
        if (_map.TryGetValue(key, out var entry))
        {
            var now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now <= entry.ExpireAt)
            {
                fileId = entry.FileId;
                return true;
            }
            _map.TryRemove(key, out _);
        }
        fileId = 0;
        return false;
    }

    public ReadOnlyMemory<byte> CreateRequestToken(ReadOnlyMemory<byte> fileToken)
    {
        var secret = GetSecret();
        using var hmac = new HMACSHA256(secret);
        var mac = hmac.ComputeHash(fileToken.ToArray());
        return mac;
    }

    public bool ValidateRequestToken(ReadOnlyMemory<byte> fileToken, ReadOnlyMemory<byte> requestToken)
    {
        var expected = CreateRequestToken(fileToken).ToArray();
        return CryptographicOperations.FixedTimeEquals(expected, requestToken.ToArray());
    }

    private byte[] GetSecret()
    {
        var s = options.CurrentValue.CdnTokenSecret;
        if (string.IsNullOrEmpty(s))
        {
            // fallback dev secret
            s = "dev-cdn-secret";
        }
        return System.Text.Encoding.UTF8.GetBytes(s);
    }
}

