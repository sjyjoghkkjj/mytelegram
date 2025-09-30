using MyTelegram.Schema;
using System.Numerics;
using System.Security.Cryptography;

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
        byte[] Salt1,
        byte[] Salt2,
        byte[] Hash,
        string? Hint,
        string? VerifiedEmail,
        string? UnconfirmedEmail,
        int? PendingResetDate,
        int? ResetRetryDate
    );
    private readonly ConcurrentDictionary<long, State> _store = new();
    private readonly ConcurrentDictionary<long, SrpState> _srp = new();

    private sealed record SrpState(long SrpId, BigInteger bSecret, byte[] BPublic);

    public Task<Account.TPassword> GetPasswordAsync(long userId)
    {
        var has = _store.TryGetValue(userId, out var state);
        var srpId = randomHelper.NextInt64();
        byte[] B;
        if (has)
        {
            B = ComputeServerPublicB(state!);
            _srp[userId] = new SrpState(srpId, _lastBSecret, B);
        }
        else
        {
            // No password set — still return random B to avoid oracle
            B = randomHelper.GenerateRandomBytes(256);
            _srp[userId] = new SrpState(srpId, BigInteger.Zero, B);
        }

        var pwd = new Account.TPassword
        {
            HasPassword = has,
            CurrentAlgo = has ? new TPasswordKdfAlgoSHA256SHA256PBKDF2HMACSHA512iter100000SHA256ModPow { Salt1 = state!.Salt1, Salt2 = state!.Salt2, G = 3, P = AuthConsts.Dh2048P } : null,
            SrpB = has ? B : null,
            SrpId = has ? srpId : null,
            Hint = state?.Hint,
            HasRecovery = !string.IsNullOrEmpty(state?.VerifiedEmail),
            HasSecureValues = false,
            NewAlgo = new TPasswordKdfAlgoSHA256SHA256PBKDF2HMACSHA512iter100000SHA256ModPow { Salt1 = RandomSalt().ToArray(), Salt2 = RandomSalt().ToArray(), G = 3, P = AuthConsts.Dh2048P },
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
            _ => new State(newAlgo.Salt1.ToArray(), newAlgo.Salt2.ToArray(), newHash, settings.Hint, null, settings.Email, null, null),
            (_, existing) => existing with
            {
                Salt1 = newAlgo.Salt1.ToArray(),
                Salt2 = newAlgo.Salt2.ToArray(),
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
            var ok = VerifySrpM1(state, srp.A, srpState.BPublic, srp.M1, srpState.bSecret);
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
            _ => new State(RandomSalt().ToArray(), RandomSalt().ToArray(), Array.Empty<byte>(), null, null, email, null, null),
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
            _ => new State(RandomSalt().ToArray(), RandomSalt().ToArray(), Array.Empty<byte>(), null, email, null, null, null),
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
            _ => new State(RandomSalt().ToArray(), RandomSalt().ToArray(), Array.Empty<byte>(), null, null, null, null, null),
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
            _ => new State(RandomSalt().ToArray(), RandomSalt().ToArray(), Array.Empty<byte>(), null, null, null, untilDate, null),
            (_, existing) => existing with { PendingResetDate = untilDate });
        return Task.CompletedTask;
    }

    public Task SetResetRetryDateAsync(long userId, int? retryDate)
    {
        _store.AddOrUpdate(userId,
            _ => new State(RandomSalt().ToArray(), RandomSalt().ToArray(), Array.Empty<byte>(), null, null, null, null, retryDate),
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
            _ => new State(RandomSalt().ToArray(), RandomSalt().ToArray(), Array.Empty<byte>(), null, null, null, null, null),
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

// ------- SRP helpers -------
partial class PasswordService
{
    private BigInteger _lastBSecret;

    private static BigInteger ToBigInteger(ReadOnlySpan<byte> bytes) => new BigInteger(bytes, isUnsigned: true, isBigEndian: true);
    private static byte[] ToBytes(BigInteger i, int size)
    {
        var b = i.ToByteArray(isUnsigned: true, isBigEndian: true);
        if (b.Length == size) return b;
        if (b.Length > size)
        {
            // trim leading zeros
            var offset = b.Length - size;
            var res = new byte[size];
            Buffer.BlockCopy(b, offset, res, 0, size);
            return res;
        }
        // pad
        var padded = new byte[size];
        Buffer.BlockCopy(b, 0, padded, size - b.Length, b.Length);
        return padded;
    }

    private static byte[] H(params ReadOnlySpan<byte>[] parts)
    {
        using var sha = SHA256.Create();
        foreach (var p in parts)
        {
            sha.TransformBlock(p.ToArray(), 0, p.Length, null, 0);
        }
        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return sha.Hash!;
    }

    private static byte[] Pad(ReadOnlySpan<byte> x, int size)
    {
        if (x.Length == size) return x.ToArray();
        if (x.Length > size)
        {
            var res = new byte[size];
            Buffer.BlockCopy(x.ToArray(), x.Length - size, res, 0, size);
            return res;
        }
        var padded = new byte[size];
        Buffer.BlockCopy(x.ToArray(), 0, padded, size - x.Length, x.Length);
        return padded;
    }

    private static (BigInteger N, BigInteger g, int size) Ng()
    {
        var N = new BigInteger(AuthConsts.Dh2048P, isUnsigned: true, isBigEndian: true);
        var g = new BigInteger(3);
        return (N, g, AuthConsts.Dh2048P.Length);
    }

    private byte[] ComputeServerPublicB(State state)
    {
        var (N, g, size) = Ng();
        // Interpret stored Hash as SRP verifier v
        var v = state.Hash.Length > 0 ? ToBigInteger(state.Hash) : BigInteger.One;
        // random secret b
        var bBytes = randomHelper.GenerateRandomBytes(size);
        var b = ToBigInteger(bBytes) % N;
        if (b.IsZero) b = new BigInteger(1);
        // k = H(N | pad(g))
        var k = ToBigInteger(H(AuthConsts.Dh2048P, ToBytes(g, size)));
        var gb = BigInteger.ModPow(g, b, N);
        var B = (k * v + gb) % N;
        if (B.IsZero) B = gb; // avoid zero
        _lastBSecret = b;
        return ToBytes(B, size);
    }

    private bool VerifySrpM1(State state, ReadOnlyMemory<byte> Abytes, ReadOnlyMemory<byte> Bbytes, ReadOnlyMemory<byte> M1bytes, BigInteger bSecret)
    {
        if (state.Hash.Length == 0) return false;
        var (N, g, size) = Ng();
        var A = ToBigInteger(Abytes.Span);
        var B = ToBigInteger(Bbytes.Span);
        if (A.IsZero || B.IsZero) return false;
        var v = ToBigInteger(state.Hash);
        // u = H(pad(A)|pad(B))
        var u = ToBigInteger(H(Pad(Abytes.Span, size), Pad(Bbytes.Span, size)));
        if (u.IsZero) return false;
        // S = (A * v^u mod N)^b mod N
        var vu = BigInteger.ModPow(v, u, N);
        var Avu = (A * vu) % N;
        var S = BigInteger.ModPow(Avu, bSecret, N);
        var K = H(ToBytes(S, size));
        var M1 = H(Pad(Abytes.Span, size), Pad(Bbytes.Span, size), K);
        return CryptographicOperations.FixedTimeEquals(M1, M1bytes.Span);
    }
}

