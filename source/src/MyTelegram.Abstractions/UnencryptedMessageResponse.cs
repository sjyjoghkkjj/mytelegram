namespace MyTelegram.Abstractions;

public record UnencryptedMessageResponse(long AuthKeyId,
    byte[] Data,
    //ReadOnlyMemory<byte> Data,
    string ConnectionId,
    long ReqMsgId);