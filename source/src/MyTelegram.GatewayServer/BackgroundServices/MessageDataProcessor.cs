namespace MyTelegram.GatewayServer.BackgroundServices;

public class MessageDataProcessor(IEventBus eventBus)
    : IDataProcessor<UnencryptedMessage>,
        IDataProcessor<EncryptedMessage>, ITransientDependency
{
    public Task ProcessAsync(EncryptedMessage data)
    {
        return eventBus.PublishAsync(data);
    }

    public Task ProcessAsync(UnencryptedMessage data)
    {
        return eventBus.PublishAsync(data);
    }
}