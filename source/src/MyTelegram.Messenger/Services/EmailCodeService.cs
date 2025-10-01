using MyTelegram.Core;

namespace MyTelegram.Messenger.Services;

public interface IEmailSender
{
    Task SendAsync(string email, string subject, string body, CancellationToken ct = default);
}

public class NullEmailSender(ILogger<NullEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string email, string subject, string body, CancellationToken ct = default)
    {
        logger.LogWarning("NullEmailSender: email won't be sent. To={Email} Subject={Subject} Body={Body}", email, subject, body);
        return Task.CompletedTask;
    }
}

public interface IEmailCodeService : ITransientDependency
{
    Task<(string code, int expire)> CreateAsync(long userId, string email, TimeSpan ttl, CancellationToken ct = default);
    Task<bool> VerifyAsync(long userId, string email, string code, CancellationToken ct = default);
    Task SetVerifiedEmailAsync(long userId, string email, CancellationToken ct = default);
    Task<string?> GetVerifiedEmailAsync(long userId, CancellationToken ct = default);
    Task SetEmailLoginEnabledAsync(long userId, bool enabled, CancellationToken ct = default);
    Task<bool> IsEmailLoginEnabledAsync(long userId, CancellationToken ct = default);
}

public class EmailCodeService(IRandomHelper randomHelper, IEmailSender emailSender, ILogger<EmailCodeService> logger, ICacheManager<EmailCodeService.FailsCacheItem> cache) : IEmailCodeService
{
    private readonly ConcurrentDictionary<long, Entry> _codes = new();
    private readonly ConcurrentDictionary<long, string> _verifiedEmails = new();
    private readonly ConcurrentDictionary<long, bool> _emailLoginEnabled = new();
    private readonly ConcurrentDictionary<(long UserId, string Scope), (int Count, int? BlockUntil, int BlockLevel)> _fails = new();
    private readonly ICacheManager<FailsCacheItem> _cache = cache;

    public async Task<(string code, int expire)> CreateAsync(long userId, string email, TimeSpan ttl, CancellationToken ct = default)
    {
        var code = randomHelper.GenerateRandomNumber(6);
        var expire = (int)DateTimeOffset.UtcNow.Add(ttl).ToUnixTimeSeconds();
        _codes[userId] = new Entry(email, code, expire);
        await emailSender.SendAsync(email, "Your login code", $"Your code: {code}", ct);
        logger.LogInformation("Email code created for user {UserId}, email {Email}", userId, email);
        return (code, expire);
    }

    public Task<bool> VerifyAsync(long userId, string email, string code, CancellationToken ct = default)
    {
        if (_codes.TryGetValue(userId, out var e))
        {
            var now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (e.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && e.Code == code && now <= e.Expire)
            {
                _codes.TryRemove(userId, out _);
                return Task.FromResult(true);
            }
        }
        return Task.FromResult(false);
    }

    public Task SetVerifiedEmailAsync(long userId, string email, CancellationToken ct = default)
    {
        _verifiedEmails[userId] = email;
        return Task.CompletedTask;
    }

    public Task<string?> GetVerifiedEmailAsync(long userId, CancellationToken ct = default)
    {
        _verifiedEmails.TryGetValue(userId, out var email);
        return Task.FromResult<string?>(email);
    }

    public Task SetEmailLoginEnabledAsync(long userId, bool enabled, CancellationToken ct = default)
    {
        _emailLoginEnabled[userId] = enabled;
        return Task.CompletedTask;
    }

    public Task<bool> IsEmailLoginEnabledAsync(long userId, CancellationToken ct = default)
    {
        return Task.FromResult(_emailLoginEnabled.TryGetValue(userId, out var enabled) && enabled);
    }

    private sealed record Entry(string Email, string Code, int Expire);

    // Brute-force protection (confirm/recover)
    public sealed record FailsCacheItem(int Count, int? BlockUntil, int BlockLevel, string Scope);

    private static string FailKey(long userId, string scope) => $"emailfails:{scope}:{userId}";

    public async Task<bool> IsBlockedAsync(long userId, string scope)
    {
        var seconds = await GetBlockSecondsAsync(userId, scope);
        return seconds > 0;
    }

    public async Task<int> GetBlockSecondsAsync(long userId, string scope)
    {
        var s = await GetFailsAsync(userId, scope);
        if (s.BlockUntil.HasValue)
        {
            var now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var left = s.BlockUntil.Value - now;
            if (left > 0)
            {
                return left;
            }
            await SaveFailsAsync(userId, scope, (0, null, 0));
        }
        return 0;
    }

    public async Task RegisterFailedAttemptAsync(long userId, string scope, int maxAttempts, int initialBlockSeconds)
    {
        var now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var entry = await GetFailsAsync(userId, scope);
        entry = (entry.Count + 1, entry.BlockUntil, entry.BlockLevel);
        if (entry.Count >= maxAttempts)
        {
            var nextLevel = Math.Clamp(entry.BlockLevel + 1, 1, 3);
            var seconds = nextLevel switch
            {
                1 => initialBlockSeconds,
                2 => 86400,
                _ => 86400 * 7
            };
            await SaveFailsAsync(userId, scope, (0, now + seconds, nextLevel));
            return;
        }
        await SaveFailsAsync(userId, scope, entry);
    }

    public async Task ResetFailedAttemptsAsync(long userId, string scope)
    {
        await SaveFailsAsync(userId, scope, (0, null, 0));
    }

    private async Task<(int Count, int? BlockUntil, int BlockLevel)> GetFailsAsync(long userId, string scope)
    {
        if (_fails.TryGetValue((userId, scope), out var inMem))
        {
            return inMem;
        }
        var cached = await _cache.GetAsync(FailKey(userId, scope));
        if (cached != null)
        {
            var r = (cached.Count, cached.BlockUntil, cached.BlockLevel);
            _fails[(userId, scope)] = r;
            return r;
        }
        return (0, null, 0);
    }

    private async Task SaveFailsAsync(long userId, string scope, (int Count, int? BlockUntil, int BlockLevel) value)
    {
        _fails[(userId, scope)] = value;
        var ttl = value.BlockUntil.HasValue ? Math.Max(60, value.BlockUntil.Value - (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()) : 3600;
        await _cache.SetAsync(FailKey(userId, scope), new FailsCacheItem(value.Count, value.BlockUntil, value.BlockLevel, scope), ttlInSeconds: ttl);
    }
}

