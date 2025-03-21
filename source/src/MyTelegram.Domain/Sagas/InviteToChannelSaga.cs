namespace MyTelegram.Domain.Sagas;

public class InviteToChannelSaga :
    MyInMemoryAggregateSaga<InviteToChannelSaga, InviteToChannelSagaId, InviteToChannelSagaLocator>,
    //ISagaIsStartedBy<ChannelAggregate, ChannelId, StartInviteToChannelEvent>,
    ISagaIsStartedBy<TempAggregate, TempId, InviteToChannelStartedEvent>,
    ISagaHandles<ChannelMemberAggregate, ChannelMemberId, ChannelMemberCreatedEvent>,
    IApply<InviteToChannelCompletedSagaEvent>
{
    private readonly IIdGenerator _idGenerator;
    private readonly InviteToChannelSagaState _state = new();

    public InviteToChannelSaga(InviteToChannelSagaId id, IEventStore eventStore, IIdGenerator idGenerator) : base(id,
        eventStore)
    {
        _idGenerator = idGenerator;
        Register(_state);
    }

    public void Apply(InviteToChannelCompletedSagaEvent aggregateEvent)
    {
        CompleteAsync();
    }

    public async Task HandleAsync(
        IDomainEvent<ChannelMemberAggregate, ChannelMemberId, ChannelMemberCreatedEvent> domainEvent,
        ISagaContext sagaContext,
        CancellationToken cancellationToken)
    {
        Emit(new InviteToChannelSagaMemberCreatedSagaEvent());
        var command = new IncrementParticipantCountCommand(ChannelId.Create(domainEvent.AggregateEvent.ChannelId));
        Publish(command);

        if (!domainEvent.AggregateEvent.IsRejoin)
        {
            var toPeer = new Peer(PeerType.Channel, domainEvent.AggregateEvent.ChannelId);
            var createDialogCommand = new CreateDialogCommand(
                DialogId.Create(domainEvent.AggregateEvent.UserId, toPeer),
                _state.RequestInfo,
                domainEvent.AggregateEvent.UserId,
                toPeer,
                _state.ChannelHistoryMinId,
                _state.MaxMessageId
            );
            Publish(createDialogCommand);
        }

        await HandleInviteToChannelCompletedAsync();
    }

    public Task HandleAsync(IDomainEvent<TempAggregate, TempId, InviteToChannelStartedEvent> domainEvent,
        ISagaContext sagaContext, CancellationToken cancellationToken)
    {
        Emit(new InviteToChannelSagaStartSagaEvent(
            domainEvent.AggregateEvent.RequestInfo,
            domainEvent.AggregateEvent.ChannelId,
            domainEvent.AggregateEvent.IsBroadcast,
            domainEvent.AggregateEvent.HasLink,
            domainEvent.AggregateEvent.InviterId,
            domainEvent.AggregateEvent.MemberUserIds,
            domainEvent.AggregateEvent.BotUserIds,
            domainEvent.AggregateEvent.ChannelHistoryMinId,
            domainEvent.AggregateEvent.MaxMessageId,
            domainEvent.AggregateEvent.ChatJoinType
        ));

        var date = DateTime.UtcNow.ToTimestamp();
        foreach (var userId in domainEvent.AggregateEvent.MemberUserIds)
        {
            var isBot = domainEvent.AggregateEvent.BotUserIds.Contains(userId);
            CreateChannelMember(_state.RequestInfo, domainEvent.AggregateEvent.ChannelId,
                userId,
                domainEvent.AggregateEvent.RequestInfo.UserId,
                isBot,
                domainEvent.AggregateEvent.IsBroadcast,
                date
            );
        }

        //foreach (var botUserId in domainEvent.AggregateEvent.BotUserIds)
        //{
        //    CreateChannelMember(_state.RequestInfo, domainEvent.AggregateEvent.ChannelId,
        //        botUserId,
        //        domainEvent.AggregateEvent.RequestInfo.UserId,
        //        false,
        //        domainEvent.AggregateEvent.IsBroadcast,
        //        date
        //    );
        //}

        return Task.CompletedTask;
    }

    private void CreateChannelMember(RequestInfo requestInfo, long channelId, long userId, long inviterUserId,
        bool isBot, bool isBroadcast, int date)
    {
        var command = new CreateChannelMemberCommand(
            ChannelMemberId.Create(channelId, userId),
            requestInfo,
            channelId,
            userId,
            inviterUserId,
            date,
            isBot,
            null,
            isBroadcast
        );
        Publish(command);
    }


    private async Task HandleInviteToChannelCompletedAsync()
    {
        if (_state.Completed)
        {
            // send service message to member after invited to super group
            //if (_state is { Broadcast: false, HasLink: false })
            if (!_state.Broadcast)
            {
                var ownerPeerId = _state.ChannelId;
                //var outMessageId = await _idGenerator.NextIdAsync(IdType.MessageId, ownerPeerId);
                var outMessageId = 0;
                //var aggregateId = MessageId.Create(ownerPeerId, outMessageId);
                var ownerPeer = new Peer(PeerType.Channel, ownerPeerId);
                var senderPeer = new Peer(PeerType.User, _state.InviterId);

                List<long> allMemberUserIds = [];
                allMemberUserIds.AddRange(_state.MemberUserIds);
                allMemberUserIds.AddRange(_state.BotUserIds);

                var messageSubType = MessageSubType.None;
                switch (_state.ChatJoinType)
                {
                    case ChatJoinType.InvitedByAdmin:
                        messageSubType = MessageSubType.InviteToChannel;
                        break;
                    case ChatJoinType.ByRequest:
                        messageSubType = MessageSubType.ChatJoinByRequest;
                        break;
                    case ChatJoinType.ByLink:
                        messageSubType = MessageSubType.ChatJoinedByLink;
                        break;
                    case ChatJoinType.ApprovedByAdmin:
                        messageSubType = MessageSubType.ChatJoinApprovedByAdmin;
                        break;
                }

                var messageItem = new MessageItem(
                    ownerPeer,
                    ownerPeer,
                    senderPeer,
                    _state.InviterId,
                    outMessageId,
                    string.Empty,
                    DateTime.UtcNow.ToTimestamp(),
                    _state.RandomId,
                    true,
                    SendMessageType.MessageService,
                    MessageType.Text,
                    messageSubType,
                    null,
                    new TMessageActionChatAddUser
                    {
                        Users = new TVector<long>(allMemberUserIds)
                    },
                    MessageActionType.ChatAddUser
                );
                var command = new StartSendMessageCommand(TempId.New,
                    _state.RequestInfo with { IsSubRequest = true },
                    [new SendMessageItem(messageItem)]);

                Publish(command);
            }

            Emit(new InviteToChannelCompletedSagaEvent(_state.RequestInfo,
                _state.ChannelId,
                _state.InviterId,
                _state.Broadcast,
                _state.MemberUserIds,
                _state.BotUserIds,
                _state.HasLink,
                _state.ChatJoinType
            ));
        }
    }
}