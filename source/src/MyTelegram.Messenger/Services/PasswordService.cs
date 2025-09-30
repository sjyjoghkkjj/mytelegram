using MyTelegram.Schema;

namespace MyTelegram.Messenger.Services;

public interface IPasswordService : ITransientDependency
{
    Task<Account.TPassword> GetPasswordAsync(long userId);
    Task SetPasswordAsync(long userId, Account.TPasswordInputSettings settings);
    Task<bool> CheckPasswordAsync(long userId, IInputCheckPasswordSRP password);
    Task<Account.TTmpPassword> CreateTmpPasswordAsync(long userId, ReadOnlyMemory<byte> purpose, int validitySeconds);

    // Email (recovery) flow
    Task SetUnconfirmedEmailAsync(long userId, string email);
    Task<string?> GetUnconfirmedEmailAsync(long userId);
    Task SetVerifiedEmailAsync(long userId, string email);
    Task<string?> GetVerifiedEmailAsync(long userId);
    Task CancelUnconfirmedEmailAsync(long userId);

    // Password settings retrieval (requires prior password check at handler level)
    Task<Account.TPasswordSettings> GetPasswordSettingsAsync(long userId);

    // Reset flow
    Task<int?> GetPendingResetDateAsync(long userId);
    Task SetPendingResetDateAsync(long userId, int? untilDate);
    Task SetResetRetryDateAsync(long userId, int? retryDate);
    Task<int?> GetResetRetryDateAsync(long userId);
    Task ClearPasswordAsync(long userId);
}

public class PasswordService(IRandomHelper randomHelper, ILogger<PasswordService> logger) : IPasswordService
{
    private sealed record State(
        byte[] Salt,
        byte[] Hash,
        string? Hint,
        string? VerifiedEmail,
        string? UnconfirmedEmail,
        int? PendingResetDate,
        int? ResetRetryDate
    );
    private readonly ConcurrentDictionary<long, State> _store = new();
    private readonly ConcurrentDictionary<long, (long SrpId, byte[] B)> _srp = new();

    public Task<Account.TPassword> GetPasswordAsync(long userId)
    {
        var has = _store.TryGetValue(userId, out var state);
        var srpId = randomHelper.NextInt64();
        var srpB = randomHelper.GenerateRandomBytes(256);
        _srp[userId] = (srpId, srpB);

        var pwd = new Account.TPassword
        {
            HasPassword = has,
            CurrentAlgo = has ? new TPasswordKdfAlgoSHA256SHA256PBKDF2HMACSHA512iter100000SHA256ModPow { Salt1 = state!.Salt, Salt2 = state!.Salt, G = 3, P = ReadOnlyMemory<byte>.Empty } : null,
            SrpB = has ? srpB : null,
            SrpId = has ? srpId : null,
            Hint = state?.Hint,
            HasRecovery = !string.IsNullOrEmpty(state?.VerifiedEmail),
            HasSecureValues = false,
            NewAlgo = new TPasswordKdfAlgoSHA256SHA256PBKDF2HMACSHA512iter100000SHA256ModPow { Salt1 = RandomSalt(), Salt2 = RandomSalt(), G = 3, P = ReadOnlyMemory<byte>.Empty },
            NewSecureAlgo = new TSecurePasswordKdfAlgoPBKDF2HMACSHA512iter100000(),
            SecureRandom = randomHelper.GenerateRandomBytes(256),
            EmailUnconfirmedPattern = string.IsNullOrEmpty(state?.UnconfirmedEmail) ? null : Obfuscate(state!.UnconfirmedEmail!).pattern,
            LoginEmailPattern = string.IsNullOrEmpty(state?.VerifiedEmail) ? null : Obfuscate(state!.VerifiedEmail!).pattern,
            PendingResetDate = state?.PendingResetDate
        };
        return Task.FromResult(pwd);
    }

    public Task SetPasswordAsync(long userId, Account.TPasswordInputSettings settings)
    {
        if (settings.NewAlgo is not TPasswordKdfAlgoSHA256SHA256PBKDF2HMACSHA512iter100000SHA256ModPow newAlgo)
        {
            RpcErrors.RpcErrors400.NewSettingsInvalid.ThrowRpcError();
        }
        var newHash = settings.NewPasswordHash?.ToArray() ?? Array.Empty<byte>();
        _store.AddOrUpdate(userId,
            _ => new State(newAlgo.Salt1.ToArray(), newHash, settings.Hint, null, settings.Email, null, null),
            (_, existing) => existing with
            {
                Salt = newAlgo.Salt1.ToArray(),
                Hash = newHash,
                Hint = settings.Hint,
                // if a new email is supplied, store it as unconfirmed until confirmation
                UnconfirmedEmail = settings.Email ?? existing.UnconfirmedEmail
            });
        logger.LogInformation("Password set for user {UserId}", userId);
        return Task.CompletedTask;
    }

    public Task<bool> CheckPasswordAsync(long userId, IInputCheckPasswordSRP password)
    {
        if (_store.TryGetValue(userId, out var state) && password is TInputCheckPasswordSRP srp && _srp.TryGetValue(userId, out var srpState) && srp.SrpId == srpState.SrpId)
        {
            // NOTE: Proper SRP check should verify M1; for now, require non-empty A/M1 and existing password.
            var hasPassword = state.Hash.Length > 0;
            var ok = hasPassword && srp.A.Length > 0 && srp.M1.Length > 0;
            return Task.FromResult(ok);
        }
        return Task.FromResult(false);
    }

    public Task<Account.TTmpPassword> CreateTmpPasswordAsync(long userId, ReadOnlyMemory<byte> purpose, int validitySeconds)
    {
        var bytes = randomHelper.GenerateRandomBytes(32);
        var tmp = new Account.TTmpPassword
        {
            TmpPassword = bytes,
            ValidUntil = (int)DateTimeOffset.UtcNow.AddSeconds(validitySeconds).ToUnixTimeSeconds()
        };
        return Task.FromResult(tmp);
    }

    private ReadOnlyMemory<byte> RandomSalt() => randomHelper.GenerateRandomBytes(16);

    public Task SetUnconfirmedEmailAsync(long userId, string email)
    {
        _store.AddOrUpdate(userId,
            _ => new State(RandomSalt().ToArray(), Array.Empty<byte>(), null, null, email, null, null),
            (_, existing) => existing with { UnconfirmedEmail = email });
        return Task.CompletedTask;
    }

    public Task<string?> GetUnconfirmedEmailAsync(long userId)
    {
        _store.TryGetValue(userId, out var state);
        return Task.FromResult<string?>(state?.UnconfirmedEmail);
    }

    public Task SetVerifiedEmailAsync(long userId, string email)
    {
        _store.AddOrUpdate(userId,
            _ => new State(RandomSalt().ToArray(), Array.Empty<byte>(), null, email, null, null, null),
            (_, existing) => existing with { VerifiedEmail = email, UnconfirmedEmail = null });
        return Task.CompletedTask;
    }

    public Task<string?> GetVerifiedEmailAsync(long userId)
    {
        _store.TryGetValue(userId, out var state);
        return Task.FromResult<string?>(state?.VerifiedEmail);
    }

    public Task CancelUnconfirmedEmailAsync(long userId)
    {
        _store.AddOrUpdate(userId,
            _ => new State(RandomSalt().ToArray(), Array.Empty<byte>(), null, null, null, null, null),
            (_, existing) => existing with { UnconfirmedEmail = null });
        return Task.CompletedTask;
    }

    public Task<Account.TPasswordSettings> GetPasswordSettingsAsync(long userId)
    {
        _store.TryGetValue(userId, out var state);
        var settings = new Account.TPasswordSettings
        {
            Email = state?.VerifiedEmail
        };
        return Task.FromResult(settings);
    }

    public Task<int?> GetPendingResetDateAsync(long userId)
    {
        _store.TryGetValue(userId, out var state);
        return Task.FromResult<int?>(state?.PendingResetDate);
    }

    public Task SetPendingResetDateAsync(long userId, int? untilDate)
    {
        _store.AddOrUpdate(userId,
            _ => new State(RandomSalt().ToArray(), Array.Empty<byte>(), null, null, null, untilDate, null),
            (_, existing) => existing with { PendingResetDate = untilDate });
        return Task.CompletedTask;
    }

    public Task SetResetRetryDateAsync(long userId, int? retryDate)
    {
        _store.AddOrUpdate(userId,
            _ => new State(RandomSalt().ToArray(), Array.Empty<byte>(), null, null, null, null, retryDate),
            (_, existing) => existing with { ResetRetryDate = retryDate });
        return Task.CompletedTask;
    }

    public Task<int?> GetResetRetryDateAsync(long userId)
    {
        _store.TryGetValue(userId, out var state);
        return Task.FromResult<int?>(state?.ResetRetryDate);
    }

    public Task ClearPasswordAsync(long userId)
    {
        _store.AddOrUpdate(userId,
            _ => new State(RandomSalt().ToArray(), Array.Empty<byte>(), null, null, null, null, null),
            (_, existing) => existing with { Hash = Array.Empty<byte>(), Hint = null, PendingResetDate = null });
        return Task.CompletedTask;
    }

    private static (string pattern) Obfuscate(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2) return ("***@***");
        string mask(string s) => s.Length <= 2 ? "**" : s[0] + new string('*', s.Length - 2) + s[^1];
        return ($"{mask(parts[0])}@{mask(parts[1])}");
    }
}

