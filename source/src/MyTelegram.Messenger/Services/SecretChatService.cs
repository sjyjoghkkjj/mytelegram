using System.Numerics;

namespace MyTelegram.Messenger.Services;

public interface ISecretChatService : ISingletonDependency
{
    EncryptedChatState CreateRequest(long adminId, long participantId, byte[] gA);
    EncryptedChatState Accept(int chatId, long accepterUserId, byte[] gB, long keyFingerprint);
    EncryptedChatState? Get(int chatId);
    long AddMessage(int chatId, long fromUserId, ReadOnlyMemory<byte> encryptedBytes, bool hasFile);
    void MarkRead(int chatId, long userId);
    IReadOnlyList<EncryptedMessageItem> GetPendingFor(long userId);
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

public sealed class EncryptedMessageItem
{
    public int ChatId { get; init; }
    public long FromUserId { get; init; }
    public long RandomId { get; init; }
    public int Date { get; init; }
    public ReadOnlyMemory<byte> Bytes { get; init; }
    public bool HasFile { get; init; }
}

public class SecretChatService(IRandomHelper randomHelper, ILogger<SecretChatService> logger) : ISecretChatService
{
    private readonly ConcurrentDictionary<int, EncryptedChatState> _chats = new();
    private readonly ConcurrentDictionary<long, List<EncryptedMessageItem>> _inbox = new();

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

    public long AddMessage(int chatId, long fromUserId, ReadOnlyMemory<byte> encryptedBytes, bool hasFile)
    {
        if (!_chats.TryGetValue(chatId, out var state))
        {
            RpcErrors.RpcErrors400.ChatIdInvalid.ThrowRpcError();
        }
        var toUser = fromUserId == state.AdminId ? state.ParticipantId : state.AdminId;
        var item = new EncryptedMessageItem
        {
            ChatId = chatId,
            FromUserId = fromUserId,
            RandomId = Random.Shared.NextInt64(),
            Date = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Bytes = encryptedBytes,
            HasFile = hasFile
        };
        var list = _inbox.GetOrAdd(toUser, _ => new List<EncryptedMessageItem>());
        list.Add(item);
        return item.RandomId;
    }

    public void MarkRead(int chatId, long userId)
    {
        _inbox.TryRemove(userId, out _);
    }

    public IReadOnlyList<EncryptedMessageItem> GetPendingFor(long userId)
    {
        return _inbox.TryGetValue(userId, out var list) ? list.ToList() : new List<EncryptedMessageItem>();
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

