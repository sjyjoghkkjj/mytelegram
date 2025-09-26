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
}

public class PasswordService(IRandomHelper randomHelper, ILogger<PasswordService> logger) : IPasswordService
{
    private sealed record State(byte[] Salt1, byte[] Hash, string? Hint);
    private sealed record SrpState(long SrpId, byte[] ServerB, byte[] ServerBPriv);
    private readonly ConcurrentDictionary<long, State> _store = new();
    private readonly ConcurrentDictionary<long, SrpState> _srp = new();

    // RFC 5054 2048-bit group N, g=3
    private static readonly byte[] SrpN = Convert.FromHexString(
        "AC6BDB41324A9A9BF166DE5E1389582FAF72B6651987EE07FC3192943DB56050A37329CBB4A099ED8193E0757767A13DD52312AB4B03310DCD7F48A9DA04FD50E8083969EDB767B0CF6096C9AB28F8A0D7C7C1B3B9A92EE1909D0D2263F80A76A6A24C087A091F531DBF0A0169B6A28AD662A4D18E73AFA3C1CAF1E9D6E9D56E7C97FBEC7E8F3B9CAF56B"
            + "E5F0F5E7F0E1CC06C3D1E08AD3EBB2C3E3F1C16C5D54AF0AD873D6C6C3E9E37ED8D05F6EDC5E0E0E6FAD8F7B7");
    private const int SrpG = 3;
    private static readonly BigInteger N = ToBigInteger(SrpN);
    private static readonly BigInteger g = new(SrpG);

    public Task<Account.TPassword> GetPasswordAsync(long userId)
    {
        var has = _store.TryGetValue(userId, out var state);
        var srpId = randomHelper.NextInt64();
        byte[]? srpB = null;
        if (has)
        {
            // Build SRP server values
            var (B, bPriv) = BuildServerEphemeral(state!);
            srpB = B;
            _srp[userId] = new SrpState(srpId, B, bPriv);
        }
        var pwd = new Account.TPassword
        {
            HasPassword = has,
            CurrentAlgo = has ? new TPasswordKdfAlgoSHA256SHA256PBKDF2HMACSHA512iter100000SHA256ModPow { Salt1 = state!.Salt1, Salt2 = state!.Salt1, G = SrpG, P = SrpN } : null,
            SrpB = has ? srpB : null,
            SrpId = has ? srpId : null,
            Hint = state?.Hint,
            HasRecovery = true,
            HasSecureValues = false,
            NewAlgo = new TPasswordKdfAlgoSHA256SHA256PBKDF2HMACSHA512iter100000SHA256ModPow { Salt1 = RandomSalt(), Salt2 = RandomSalt(), G = SrpG, P = SrpN },
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
            // Compute expected M1 using SRP-6a
            var A = ToBigInteger(srp.A.ToArray());
            var B = ToBigInteger(srpState.ServerB);
            if (A % N == 0 || B % N == 0) return Task.FromResult(false);

            var k = HBigInt(Concat(SrpN, IntToBytes(SrpG)));
            var u = HBigInt(Concat(ToBigEndian(A), ToBigEndian(B)));

            var x = HBigInt(Concat(state.Salt1, state.Hash, state.Salt1));
            var v = BigInteger.ModPow(g, x, N);

            var b = ToBigInteger(srpState.ServerBPriv);
            var S = BigInteger.ModPow((A * BigInteger.ModPow(v, u, N)) % N, b, N);
            var K = SHA256Hash(ToBigEndian(S));
            var M1Expected = SHA256Hash(Concat(ToBigEndian(A), ToBigEndian(B), K));
            var ok = srp.M1.Span.SequenceEqual(M1Expected);
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

    private static (byte[] B, byte[] bPriv) BuildServerEphemeral(State state)
    {
        // b
        var bPriv = RandomBytes(256);
        var b = ToBigInteger(bPriv);
        // v
        var x = HBigInt(Concat(state.Salt1, state.Hash, state.Salt1));
        var v = BigInteger.ModPow(g, x, N);
        var k = HBigInt(Concat(SrpN, IntToBytes(SrpG)));
        var gb = BigInteger.ModPow(g, b, N);
        var B = (k * v + gb) % N;
        return (ToBigEndian(B), bPriv);
    }

    private static byte[] RandomBytes(int len)
    {
        var buf = new byte[len];
        RandomNumberGenerator.Fill(buf);
        return buf;
    }

    private static BigInteger ToBigInteger(byte[] be)
    {
        var le = (byte[])be.Clone();
        Array.Reverse(le);
        var extended = new byte[le.Length + 1];
        Array.Copy(le, extended, le.Length);
        return new BigInteger(extended);
    }

    private static byte[] ToBigEndian(BigInteger bi)
    {
        var le = bi.ToByteArray();
        Array.Reverse(le);
        // Trim leading zeros
        int i = 0;
        while (i < le.Length - 1 && le[i] == 0) i++;
        if (i == 0) return le;
        var be = new byte[le.Length - i];
        Array.Copy(le, i, be, 0, be.Length);
        return be;
    }

    private static byte[] IntToBytes(int v)
    {
        var b = BitConverter.GetBytes(v);
        if (BitConverter.IsLittleEndian) Array.Reverse(b);
        return b;
    }

    private static byte[] Concat(params byte[][] arrays)
    {
        var len = arrays.Sum(a => a.Length);
        var buf = new byte[len];
        int offset = 0;
        foreach (var a in arrays)
        {
            Buffer.BlockCopy(a, 0, buf, offset, a.Length);
            offset += a.Length;
        }
        return buf;
    }

    private static BigInteger HBigInt(byte[] data)
    {
        using var sha = SHA256.Create();
        var h = sha.ComputeHash(data);
        return ToBigInteger(h);
    }

    private static byte[] SHA256Hash(byte[] data)
    {
        using var sha = SHA256.Create();
        return sha.ComputeHash(data);
    }
}

