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

public class EmailCodeService(IRandomHelper randomHelper, IEmailSender emailSender, ILogger<EmailCodeService> logger) : IEmailCodeService
{
    private readonly ConcurrentDictionary<long, Entry> _codes = new();
    private readonly ConcurrentDictionary<long, string> _verifiedEmails = new();
    private readonly ConcurrentDictionary<long, bool> _emailLoginEnabled = new();

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
}

