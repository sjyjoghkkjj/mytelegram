namespace MyTelegram.GatewayServer.BackgroundServices;

public class MessageDataProcessor(IEventBus eventBus)
    : IDataProcessor<UnencryptedMessage>,
        IDataProcessor<EncryptedMessage>
{
    public Task ProcessAsync(EncryptedMessage data)
    {
        return eventBus.PublishAsync(new Core.EncryptedMessage(data.AuthKeyId, data.MsgKey, data.EncryptedData,
            data.ConnectionId,
            (ConnectionType)data.ConnectionType,
            data.ClientIp, data.RequestId, data.Date));
    }

    public Task ProcessAsync(UnencryptedMessage data)
    {
        return eventBus.PublishAsync(new Core.UnencryptedMessage(data.AuthKeyId, data.ClientIp, data.ConnectionId,
            (ConnectionType)data.ConnectionType,
            data.MessageData, data.MessageDataLength, data.MessageId, data.ObjectId, data.RequestId, data.Date));
    }
}