namespace MyTelegram.Domain.Sagas;

public class EditExportedChatInviteSaga(
    EditExportedChatInviteSagaId id,
    IEventStore eventStore,
    IIdGenerator idGenerator)
    : MyInMemoryAggregateSaga<EditExportedChatInviteSaga, EditExportedChatInviteSagaId,
            EditExportedChatInviteSagaLocator>(id, eventStore),
        ISagaIsStartedBy<ChatInviteAggregate, ChatInviteId, ChatInviteEditedEvent>
{
    public async Task HandleAsync(IDomainEvent<ChatInviteAggregate, ChatInviteId, ChatInviteEditedEvent> domainEvent, ISagaContext sagaContext, CancellationToken cancellationToken)
    {
        if (domainEvent.AggregateEvent.Revoked)
        {
            var inviteId = await idGenerator.NextLongIdAsync(IdType.InviteId, domainEvent.AggregateEvent.ChannelId, cancellationToken: cancellationToken);
            var command = new CreateChatInviteCommand(
                ChatInviteId.Create(domainEvent.AggregateEvent.ChannelId, inviteId),
                domainEvent.AggregateEvent.RequestInfo with { ReqMsgId = 0 },
                domainEvent.AggregateEvent.ChannelId,
                inviteId,
                domainEvent.AggregateEvent.NewHash!,
                domainEvent.AggregateEvent.AdminId,
                domainEvent.AggregateEvent.Title,
                domainEvent.AggregateEvent.RequestNeeded,
                domainEvent.AggregateEvent.StartDate,
                domainEvent.AggregateEvent.ExpireDate,
                domainEvent.AggregateEvent.UsageLimit,
                domainEvent.AggregateEvent.Permanent,
                DateTime.UtcNow.ToTimestamp(),
                domainEvent.AggregateEvent.IsBroadcast
            );
            Publish(command);
        }

        await CompleteAsync(cancellationToken);
    }
}