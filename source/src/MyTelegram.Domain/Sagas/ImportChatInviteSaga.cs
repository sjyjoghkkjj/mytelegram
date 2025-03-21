namespace MyTelegram.Domain.Sagas;

public class ImportChatInviteSaga(ImportChatInviteSagaId id, IEventStore eventStore, IIdGenerator idGenerator) :
    MyInMemoryAggregateSaga<ImportChatInviteSaga, ImportChatInviteSagaId, ImportChatInviteSagaLocator>(id, eventStore),
    ISagaIsStartedBy<ChatInviteAggregate, ChatInviteId, ChatInviteImportedEvent>,
    ISagaHandles<ChannelMemberAggregate, ChannelMemberId, ChannelMemberCreatedEvent>
{
    public async Task HandleAsync(IDomainEvent<ChatInviteAggregate, ChatInviteId, ChatInviteImportedEvent> domainEvent, ISagaContext sagaContext, CancellationToken cancellationToken)
    {
        if (domainEvent.AggregateEvent.ChatInviteRequestState == ChatInviteRequestState.NoApprovalRequired)
        {
            var command = new CreateChannelMemberCommand(
                ChannelMemberId.Create(domainEvent.AggregateEvent.ChannelId,
                    domainEvent.AggregateEvent.RequestInfo.UserId),
                domainEvent.AggregateEvent.RequestInfo,
                domainEvent.AggregateEvent.ChannelId,
                domainEvent.AggregateEvent.RequestInfo.UserId,
                domainEvent.AggregateEvent.AdminId,
                DateTime.UtcNow.ToTimestamp(),
                false, // Bot can only be invited by admin
                domainEvent.AggregateEvent.InviteId,
                domainEvent.AggregateEvent.IsBroadcast,
                ChatJoinType.ByLink
            );
            Publish(command);

        }
        else
        {
            var updateChatInviteRequestPendingCommand = new UpdateChatInviteRequestPendingCommand(ChannelId.Create(domainEvent.AggregateEvent.ChannelId),
                domainEvent.AggregateEvent.RequestInfo.UserId);
            Publish(updateChatInviteRequestPendingCommand);

            await CompleteAsync(cancellationToken);
        }
    }

    public async Task HandleAsync(IDomainEvent<ChannelMemberAggregate, ChannelMemberId, ChannelMemberCreatedEvent> domainEvent, ISagaContext sagaContext, CancellationToken cancellationToken)
    {
        if (!domainEvent.AggregateEvent.IsBroadcast)
        {
            var ownerPeerId = domainEvent.AggregateEvent.ChannelId;
            var outMessageId = await idGenerator.NextIdAsync(IdType.MessageId, ownerPeerId, cancellationToken: cancellationToken);
            var ownerPeer = ownerPeerId.ToChannelPeer();

            var messageItem = new MessageItem(
                ownerPeer,
                ownerPeer,
                ownerPeer,
                domainEvent.AggregateEvent.UserId,
                outMessageId,
                string.Empty,
                DateTime.UtcNow.ToTimestamp(),
                Random.Shared.NextInt64(),
                true,
                SendMessageType.MessageService,
                MessageType.Text,
                MessageSubType.ChatJoinedByLink,
                MessageAction: new TMessageActionChatJoinedByLink
                {
                    InviterId = domainEvent.AggregateEvent.InviterId
                },
                MessageActionType: MessageActionType.ChatJoinedByLink
            );
            var command = new StartSendMessageCommand(TempId.New,
                domainEvent.AggregateEvent.RequestInfo,
                [new SendMessageItem(messageItem)]);
            Publish(command);
        }

        await CompleteAsync(cancellationToken);
    }
}