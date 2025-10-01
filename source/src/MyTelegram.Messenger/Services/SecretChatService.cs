using System.Numerics;

namespace MyTelegram.Messenger.Services;

public interface ISecretChatService : ISingletonDependency
{
    EncryptedChatState CreateRequest(long adminId, long participantId, byte[] gA);
    EncryptedChatState Accept(int chatId, long accepterUserId, byte[] gB, long keyFingerprint);
    EncryptedChatState? Get(int chatId);
}

public sealed class EncryptedChatState
{
    public int ChatId { get; init; }
    public long AccessHash { get; init; }
    public int Date { get; init; }
    public long AdminId { get; init; }
    public long ParticipantId { get; init; }
    public byte[]? GA { get; set; }
    public byte[]? GB { get; set; }
    public long? KeyFingerprint { get; set; }
}

public class SecretChatService(IRandomHelper randomHelper, ILogger<SecretChatService> logger) : ISecretChatService
{
    private readonly ConcurrentDictionary<int, EncryptedChatState> _chats = new();

    public EncryptedChatState CreateRequest(long adminId, long participantId, byte[] gA)
    {
        ValidatePublic(gA);
        var chatId = unchecked((int)(randomHelper.NextInt64() & 0x7FFFFFFF));
        var state = new EncryptedChatState
        {
            ChatId = chatId,
            AccessHash = randomHelper.NextInt64(),
            Date = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            AdminId = adminId,
            ParticipantId = participantId,
            GA = gA
        };
        _chats[chatId] = state;
        return state;
    }

    public EncryptedChatState Accept(int chatId, long accepterUserId, byte[] gB, long keyFingerprint)
    {
        if (!_chats.TryGetValue(chatId, out var state))
        {
            throw RpcErrors.RpcErrors400.ChatIdInvalid.ToException();
        }
        if (accepterUserId != state.ParticipantId && accepterUserId != state.AdminId)
        {
            throw RpcErrors.RpcErrors400.ChatIdInvalid.ToException();
        }
        ValidatePublic(gB);
        state.GB = gB;
        state.KeyFingerprint = keyFingerprint;
        return state;
    }

    public EncryptedChatState? Get(int chatId)
    {
        _chats.TryGetValue(chatId, out var s);
        return s;
    }

    private static void ValidatePublic(byte[] gX)
    {
        // Minimal DH check: 1 < g^x < p-1 and length ~ 256 bytes
        if (gX == null || gX.Length < 256)
        {
            RpcErrors.RpcErrors400.DhGaInvalid.ThrowRpcError();
        }
        var p = new BigInteger(AuthConsts.Dh2048P, isUnsigned: true, isBigEndian: true);
        var gx = new BigInteger(gX, isUnsigned: true, isBigEndian: true);
        if (gx <= BigInteger.One || gx >= p - BigInteger.One)
        {
            RpcErrors.RpcErrors400.DhGaInvalid.ThrowRpcError();
        }
    }
}

