namespace MyTelegram.Abstractions;

public record EncryptedMessageResponse(long AuthKeyId,
    //byte[] Data,
    ReadOnlyMemory<byte> Data,
    string ConnectionId,
    long SeqNumber
);