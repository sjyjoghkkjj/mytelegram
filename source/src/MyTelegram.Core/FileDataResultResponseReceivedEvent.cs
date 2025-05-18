namespace MyTelegram.Core;

public record FileDataResultResponseReceivedEvent(
    long ReqMsgId,
    //ReadOnlyMemory<byte> Data
    byte[] Data
) : ISessionMessage;