namespace MyTelegram.Abstractions;

public record EncryptedMessage(long AuthKeyId,
    byte[] MsgKey,
    byte[] EncryptedData,
    string ConnectionId,
    ConnectionType ConnectionType,
    string ClientIp,
    Guid RequestId,
    long Date
) : IMtpMessage
{
    public string ConnectionId { get; set; } = ConnectionId;
    public ConnectionType ConnectionType { get; set; } = ConnectionType;
    public string ClientIp { get; set; } = ClientIp;
}