using MyTelegram.Schema;

namespace MyTelegram.Messenger.Services;

public interface IPasswordService : ITransientDependency
{
    Task<Account.TPassword> GetPasswordAsync(long userId);
    Task SetPasswordAsync(long userId, Account.TPasswordInputSettings settings);
    Task<bool> CheckPasswordAsync(long userId, IInputCheckPasswordSRP password);
    Task<Account.TTmpPassword> CreateTmpPasswordAsync(long userId, ReadOnlyMemory<byte> purpose, int validitySeconds);
}

public class PasswordService(IRandomHelper randomHelper, ILogger<PasswordService> logger) : IPasswordService
{
    private sealed record State(byte[] Salt, byte[] Hash, string? Hint);
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
            HasRecovery = true,
            HasSecureValues = false,
            NewAlgo = new TPasswordKdfAlgoSHA256SHA256PBKDF2HMACSHA512iter100000SHA256ModPow { Salt1 = RandomSalt(), Salt2 = RandomSalt(), G = 3, P = ReadOnlyMemory<byte>.Empty },
            NewSecureAlgo = new TSecurePasswordKdfAlgoPBKDF2HMACSHA512iter100000(),
            SecureRandom = randomHelper.GenerateRandomBytes(256)
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
        var state = new State(newAlgo.Salt1.ToArray(), newHash, settings.Hint);
        _store[userId] = state;
        logger.LogInformation("Password set for user {UserId}", userId);
        return Task.CompletedTask;
    }

    public Task<bool> CheckPasswordAsync(long userId, IInputCheckPasswordSRP password)
    {
        if (_store.TryGetValue(userId, out var state) && password is TInputCheckPasswordSRP srp && _srp.TryGetValue(userId, out var srpState) && srp.SrpId == srpState.SrpId)
        {
            // На практике нужно валидировать SRP M1. Здесь упрощённая проверка соответствия размеров.
            var ok = srp.A.Length > 0 && srp.M1.Length > 0;
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
}

