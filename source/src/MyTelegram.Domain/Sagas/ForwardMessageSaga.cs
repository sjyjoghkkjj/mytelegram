namespace MyTelegram.Domain.Sagas;

public class MessageReplyCreatedSagaEvent(long postChannelId, int postMessageId, long channelId, int messageId)
    : AggregateEvent<ForwardMessageSaga, ForwardMessageSagaId>
{
    public long PostChannelId { get; } = postChannelId;
    public int PostMessageId { get; } = postMessageId;
    public long ChannelId { get; } = channelId;
    public int MessageId { get; } = messageId;
}

public class ForwardMessageSaga : MyInMemoryAggregateSaga<ForwardMessageSaga, ForwardMessageSagaId, ForwardMessageSagaLocator>,
        ISagaIsStartedBy<TempAggregate, TempId, ForwardMessagesStartedEvent>,
        ISagaHandles<MessageAggregate, MessageId, MessageForwardedEvent>
{
    private readonly IIdGenerator _idGenerator;
    private readonly ForwardMessageState _state = new();

    public ForwardMessageSaga(ForwardMessageSagaId id, IEventStore eventStore, IIdGenerator idGenerator) : base(id, eventStore)
    {
        _idGenerator = idGenerator;
        Register(_state);
    }

    public async Task HandleAsync(IDomainEvent<MessageAggregate, MessageId, MessageForwardedEvent> domainEvent,
        ISagaContext sagaContext,
        CancellationToken cancellationToken)
    {
        var outMessageId = await SendMessageToTargetPeerAsync(domainEvent.AggregateEvent);
        Emit(new ForwardSingleMessageSuccessSagaEvent());

        if (_state.ForwardFromLinkedChannel)
        {
            Emit(new MessageReplyCreatedSagaEvent(domainEvent.AggregateEvent.OriginalMessageItem.ToPeer.PeerId, domainEvent.AggregateEvent.OriginalMessageItem.MessageId, _state.ToPeer.PeerId, outMessageId));
        }

        await HandleForwardCompletedAsync();
    }


    public Task HandleAsync(IDomainEvent<TempAggregate, TempId, ForwardMessagesStartedEvent> domainEvent, ISagaContext sagaContext, CancellationToken cancellationToken)
    {
        Emit(new ForwardMessageSagaStartedSagaEvent(domainEvent.AggregateEvent.RequestInfo,
            domainEvent.AggregateEvent.FromPeer,
            domainEvent.AggregateEvent.ToPeer,
            domainEvent.AggregateEvent.MessageIds,
            domainEvent.AggregateEvent.RandomIds,
            domainEvent.AggregateEvent.ForwardFromLinkedChannel,
            domainEvent.AggregateEvent.Post,
            domainEvent.AggregateEvent.TtlPeriod,
            domainEvent.AggregateEvent.FromNames
        ));
        ForwardMessage(domainEvent.AggregateEvent);
        return Task.CompletedTask;
    }

    private void ForwardMessage(ForwardMessagesStartedEvent aggregateEvent)
    {
        var ownerPeerId = _state.FromPeer.PeerType == PeerType.Channel
            ? _state.FromPeer.PeerId
            : _state.RequestInfo.UserId;
        var index = 0;
        foreach (var messageId in aggregateEvent.MessageIds)
        {
            var randomId = aggregateEvent.RandomIds[index];
            var command = new ForwardMessageCommand(MessageId.Create(ownerPeerId, messageId),
                aggregateEvent.RequestInfo,
                randomId);
            Publish(command);
            index++;
        }
    }

    private Task HandleForwardCompletedAsync()
    {
        if (_state.IsCompleted)
        {
            return CompleteAsync();
        }

        return Task.CompletedTask;
    }

    private async Task<int> SendMessageToTargetPeerAsync(MessageForwardedEvent aggregateEvent)
    {
        var item = aggregateEvent.OriginalMessageItem;
        var isForwardToSavedMessages = _state.ToPeer.PeerId == _state.RequestInfo.UserId;

        var selfUserId = _state.RequestInfo.UserId;
        var ownerPeerId = _state.ToPeer.PeerType == PeerType.Channel
            ? _state.ToPeer.PeerId
            : selfUserId;

        //Peer? fromId = null;
        //Peer? peerId = null;
        Peer? savedPeerId;
        var senderPeer = new Peer(PeerType.User, _state.RequestInfo.UserId);
        bool savedOut = false;
        var fwdFromId = _state.FromPeer;
        string? fromName = null;
        var fwdDate = item.Date;
        var channelPost = _state.FromPeer.PeerType == PeerType.Channel ? aggregateEvent.OriginalMessageItem.MessageId : 0;
        var postAuthor = item.PostAuthor;
        Peer? fwdSavedFromPeer = null;
        int? fwdSavedFromMsgId = null;
        Peer? fwdSavedFromId = null;
        string? fwdSavedFromName = null;
        int? fwdSavedDate = null;

        var isOut = true;
        MessageReply? reply = null;
        long? postChannelId = null;
        int? postMessageId = null;
        Peer? sendAs = null;

        _state.FromNames?.TryGetValue(item.SenderUserId, out fromName);

        // Forward from user peer
        if (_state.FromPeer.PeerType == PeerType.User &&
            item.SenderPeer.PeerId == aggregateEvent.RequestInfo.UserId)
        {
            fwdFromId = item.SenderPeer;
            fromName = null;
        }

        if (_state.ForwardFromLinkedChannel)
        {
            fwdSavedFromPeer = _state.FromPeer;
            fwdSavedFromMsgId = aggregateEvent.OriginalMessageItem.MessageId;
            senderPeer = _state.FromPeer;
            isOut = false;
            reply = aggregateEvent.OriginalMessageItem.Reply;
            postChannelId = aggregateEvent.OriginalMessageItem.ToPeer.PeerId;
            postMessageId = aggregateEvent.OriginalMessageItem.MessageId;
            sendAs = aggregateEvent.OriginalMessageItem.SendAs;
        }

        if (isForwardToSavedMessages)
        {
            savedOut = item.IsOut;
            savedPeerId = _state.FromPeer;
            fwdSavedFromPeer = _state.FromPeer;
            fwdSavedFromMsgId = item.MessageId;

            if (item.FwdHeader != null)
            {
                fwdFromId = item.FwdHeader.FromId;
                fwdSavedFromId = item.FwdHeader.SavedFromId;
                fwdSavedDate = item.FwdHeader.Date;
            }
        }
        else
        {
            savedPeerId = null;
        }

        if (!string.IsNullOrEmpty(fromName))
        {
            fwdFromId = null;
            savedPeerId = MyTelegramServerDomainConsts.AnonymousUserId.ToUserPeer();
        }

        var fwdHeader = new MessageFwdHeader(
            false,
            savedOut,
            fwdFromId,
            fromName,
            channelPost,
            postAuthor,
            fwdDate,
            fwdSavedFromPeer,
            fwdSavedFromMsgId,
            fwdSavedFromId,
            fwdSavedFromName,
            fwdSavedDate,
            null,
            _state.ForwardFromLinkedChannel);

        var outMessageId = await _idGenerator.NextIdAsync(IdType.MessageId, ownerPeerId);

        var ownerPeer = _state.ToPeer.PeerType == PeerType.Channel
            ? _state.ToPeer
            : senderPeer;
        var toPeer = _state.ToPeer;

        var messageItem = new MessageItem(
            ownerPeer,
            toPeer,
            senderPeer,
            aggregateEvent.OriginalMessageItem.SenderUserId,
            outMessageId,
            item.Message,
            DateTime.UtcNow.ToTimestamp(),
            aggregateEvent.RandomId,
            isOut,
            SendMessageType.Text,
            MessageType.Text,
            MessageSubType.ForwardMessage,
            //InputReplyTo: item.InputReplyTo,
            Entities: item.Entities,
            Media: item.Media,
            FwdHeader: fwdHeader,
            Views: item.Views,
            PollId: item.PollId,
            EditHide: _state.ForwardFromLinkedChannel,
            Reply: reply,
            IsForwardFromChannelPost: _state.ForwardFromLinkedChannel,
            PostChannelId: postChannelId,
            PostMessageId: postMessageId,
            Post: _state.Post,
            SendAs: sendAs,
            TtlPeriod: _state.TtlPeriod,
            Pinned: _state.ForwardFromLinkedChannel,
            Silent: aggregateEvent.OriginalMessageItem.Silent,
            SavedPeerId: savedPeerId,
            PostAuthor: aggregateEvent.OriginalMessageItem.PostAuthor
        );

        var reqMsgId = _state.ForwardFromLinkedChannel ? 0 : _state.RequestInfo.ReqMsgId;

        // Only the first message uses reqMsgId to reply to the client's RPC request
        // Other messages are sent to the client via online push
        // If reqMsgId==0, a push message will be sent to the target peer
        if (_state.ForwardCount > 0)
        {
            reqMsgId = 0;
        }

        var command = new StartSendMessageCommand(TempId.New, aggregateEvent.RequestInfo with
        {
            RequestId = Guid.NewGuid(),
            ReqMsgId = reqMsgId
        },
            [new SendMessageItem(messageItem)]);

        Publish(command);

        return outMessageId;
    }
}
