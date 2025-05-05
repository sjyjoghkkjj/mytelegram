namespace MyTelegram.Services.Services;

public class SessionMessageDataProcessor(IEventBus eventBus) : IDataProcessor<ISessionMessage>
{
    public async Task ProcessAsync(ISessionMessage data)
    {
        switch (data)
        {
            case DataResultResponseReceivedEvent dataResultResponseReceivedEvent:
                await eventBus.PublishAsync(dataResultResponseReceivedEvent);
                break;
            case DataResultResponseWithUserIdReceivedEvent dataResultResponseWithUserIdReceivedEvent:
                await eventBus.PublishAsync(dataResultResponseWithUserIdReceivedEvent);
                break;
            case FileDataResultResponseReceivedEvent fileDataResultResponseReceivedEvent:
                await eventBus.PublishAsync(fileDataResultResponseReceivedEvent);
                break;
            case LayeredAuthKeyIdMessageCreatedIntegrationEvent layeredAuthKeyIdMessageCreatedIntegrationEvent:
                await eventBus.PublishAsync(layeredAuthKeyIdMessageCreatedIntegrationEvent);
                break;
            case LayeredPushMessageCreatedIntegrationEvent layeredPushMessageCreatedIntegrationEvent:
                await eventBus.PublishAsync(layeredPushMessageCreatedIntegrationEvent);
                break;
            case PushMessageToPeerEvent pushMessageToPeerEvent:
                await eventBus.PublishAsync(pushMessageToPeerEvent);
                break;
            //case PushSessionMessageToAuthKeyIdEvent pushSessionMessageToAuthKeyIdEvent:
            //    await _eventBus.PublishAsync(pushSessionMessageToAuthKeyIdEvent);
            //    break;
            //case PushSessionMessageToPeerEvent pushSessionMessageToPeerEvent:
            //    await _eventBus.PublishAsync(pushSessionMessageToPeerEvent);
            //    break;


            default:
                throw new ArgumentOutOfRangeException(nameof(data));
        }

        //return Task.CompletedTask;
    }
}