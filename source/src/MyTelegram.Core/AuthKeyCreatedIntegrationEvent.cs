namespace MyTelegram.Core;

public record AuthKeyCreatedIntegrationEvent(
    string ConnectionId,
    long ReqMsgId,
    byte[] Data,
    long ServerSalt,
    bool IsPermanent,
    byte[] SetClientDhParamsAnswer,
    int? DcId
);