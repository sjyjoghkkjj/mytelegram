using MyTelegram.Messenger.DomainEventHandlers;

namespace MyTelegram.Messenger.CommandServer.DomainEventHandlers;

public class UserDomainEventHandler(
    IObjectMessageSender objectMessageSender,
    ICommandBus commandBus,
    IIdGenerator idGenerator,
    IAckCacheService ackCacheService,
    IMessageAppService messageAppService,
    IOptionsMonitor<MyTelegramMessengerServerOptions> options,
    IRandomHelper randomHelper)
    : DomainEventHandlerBase(objectMessageSender, commandBus, idGenerator, ackCacheService),
        ISubscribeSynchronousTo<UserAggregate, UserId, UserCreatedEvent>
{
    private readonly ICommandBus _commandBus = commandBus;

    public async Task HandleAsync(IDomainEvent<UserAggregate, UserId, UserCreatedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        if (options.CurrentValue.SetPremiumToTrueAfterUserCreated)
        {
            var command = new UpdateUserPremiumStatusCommand(domainEvent.AggregateIdentity, true);
            await _commandBus.PublishAsync(command, default);
        }

        if (!options.CurrentValue.SendWelcomeMessageAfterUserSignIn)
        {
            return;
        }

        if (!domainEvent.AggregateEvent.Bot)
        {
            var welcomeMessage = "Welcome to use MyTelegram!";
            var sendMessageInput = new SendMessageInput(new RequestInfo(0,
                    MyTelegramServerDomainConsts.OfficialUserId,
                    domainEvent.AggregateEvent.RequestInfo.AuthKeyId,
                    domainEvent.AggregateEvent.RequestInfo.PermAuthKeyId,
                    Guid.NewGuid(), 0, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    DeviceType.Desktop
                    ),
                MyTelegramServerDomainConsts.OfficialUserId,
                new Peer(PeerType.User, domainEvent.AggregateEvent.UserId/*, domainEvent.AggregateEvent.AccessHash*/),
                welcomeMessage,
                randomHelper.NextInt64());

            await messageAppService.SendMessageAsync([sendMessageInput]);
        }
    }
}