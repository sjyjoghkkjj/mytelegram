using MyTelegram.Messenger.Services.Caching;

namespace MyTelegram.Messenger.CommandServer.DomainEventHandlers;

public class AccessHashDomainEventHandler(IAccessHashHelper accessHashHelper) :
    ISubscribeSynchronousTo<UserAggregate, UserId, UserCreatedEvent>,
    ISubscribeSynchronousTo<ChannelAggregate, ChannelId, ChannelCreatedEvent>
{
    public Task HandleAsync(IDomainEvent<UserAggregate, UserId, UserCreatedEvent> domainEvent, CancellationToken cancellationToken)
    {
        accessHashHelper.AddAccessHash(domainEvent.AggregateEvent.UserId, domainEvent.AggregateEvent.AccessHash);
        return Task.CompletedTask;
    }

    public Task HandleAsync(IDomainEvent<ChannelAggregate, ChannelId, ChannelCreatedEvent> domainEvent, CancellationToken cancellationToken)
    {
        accessHashHelper.AddAccessHash(domainEvent.AggregateEvent.ChannelId, domainEvent.AggregateEvent.AccessHash);
        return Task.CompletedTask;
    }
}