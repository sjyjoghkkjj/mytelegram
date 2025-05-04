namespace MyTelegram.Core;

public record DataResultResponseReceivedEvent(
    long ReqMsgId,
    //ReadOnlyMemory<byte> Data
    byte[] Data
) : ISessionMessage;