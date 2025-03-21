namespace MyTelegram.Messenger.Converters.ConverterServices;

public class UpdatesConverterService(
    //ILayeredService<IMessageConverter> messageLayeredService,
    IMessageConverterService messageConverterService,
    IChatConverterService chatConverterService,
    IMessageResponseService messageResponseService,
    ILayeredService<IDraftMessageConverter> draftMessageLayeredService) : IUpdatesConverterService, ITransientDependency
{

    public IUpdates ToChannelMessageUpdates(long selfUserId, SendOutboxMessageCompletedSagaEvent aggregateEvent, int layer, bool mentioned = false)
    {
        if (aggregateEvent.IsSendGroupedMessages)
        {
            return CreateSendGroupedChannelMessageUpdates(selfUserId, aggregateEvent, layer);
        }

        var item = aggregateEvent.MessageItems.First();
        // selfUser==-1 means the updates is for channel member except sender
        //const int selfUserId = -1;
        var message = messageConverterService
            .ToMessage(
                selfUserId,
                //selfUserId,
                aggregateEvent.MessageItem,
                mentioned: mentioned,
                layer: layer
            );
        var updateNewChannelMessage =
            new TUpdateNewChannelMessage
            {
                Message = message,
                Pts = item.Pts,
                PtsCount = 1
            };

        return new TUpdates
        {
            Chats = [],
            Date = item.Date,
            Seq = 0,
            Users = [],
            Updates = new TVector<IUpdate>(updateNewChannelMessage)
        };
    }

    private IUpdates CreateSendGroupedChannelMessageUpdates(long selfUserId, SendOutboxMessageCompletedSagaEvent aggregateEvent, int layer)
    {
        List<IUpdate> updateList = aggregateEvent.MessageItems.Select(p => (IUpdate)new TUpdateMessageID
        {
            Id = p.MessageId,
            RandomId = p.RandomId,
        }).ToList();

        // selfUser==-1 means the updates is for channel member except sender
        //const int selfUserId = -1;
        foreach (var item in aggregateEvent.MessageItems)
        {
            var updateReadChannelInbox = new TUpdateReadChannelInbox
            {
                ChannelId = item.ToPeer.PeerId,
                MaxId = item.MessageId,
                Pts = item.Pts,
            };
            var updateNewChannelMessage = new TUpdateNewChannelMessage
            {
                Message = messageConverterService.ToMessage(selfUserId, item, layer: layer),
                Pts = item.Pts,
                PtsCount = 1
            };

            updateList.Add(updateReadChannelInbox);
            updateList.Add(updateNewChannelMessage);
        }

        return new TUpdates
        {
            Updates = new TVector<IUpdate>(updateList),
            Chats = [],
            Date = DateTime.UtcNow.ToTimestamp(),
            Users = []
        };
    }
    public IUpdates ToChannelUpdates(long selfUserId, IChannelReadModel channelReadModel,
        IPhotoReadModel? photoReadModel, int layer)
    {
        //var channel = GetChatConverter().ToChannel(selfUserId, channelReadModel, photoReadModel, null, false);
        var channel = chatConverterService.ToChannel(selfUserId, channelReadModel, photoReadModel, null, false, layer);

        return new TUpdates
        {
            Chats = new TVector<IChat>(channel),
            Updates = new TVector<IUpdate>(new TUpdateChannel { ChannelId = channelReadModel.ChannelId }),
            Users = [],
            Date = DateTime.UtcNow.ToTimestamp(),
            Seq = 0
        };
    }

    public async Task<IUpdates> ToCreateChannelUpdatesAsync(ChannelCreatedEvent eventData,
        SendOutboxMessageCompletedSagaEvent aggregateEvent, bool createUpdatesForSelf, int layer)
    {
        var channelId = eventData.ChannelId;
        var item = aggregateEvent.MessageItem;
        var updateList = ToChannelMessageServiceUpdates(item.MessageId,
            eventData.RandomId,
            item.Pts,
            null,
            item.OwnerPeer with { PeerType = PeerType.Channel },
            new TMessageActionChannelCreate { Title = eventData.Title },
            item.Date,
            0,
            createUpdatesForSelf,
            layer
            );
        var updateChannel = new TUpdateChannel { ChannelId = eventData.ChannelId };
        updateList.Insert(1, updateChannel);
        //var channel = GetChatConverter().ToChannel(eventData);
        var channel = await chatConverterService.GetChannelAsync(eventData.RequestInfo.UserId, channelId, false, false,
            layer);

        var updates = new TUpdates
        {
            Chats = new TVector<IChat>(channel),
            Date = item.Date,
            Updates = new TVector<IUpdate>(updateList),
            Users = []
        };
        return updates;
    }

    public IUpdates ToDeleteMessagesUpdates(PeerType toPeerType,
        DeletedBoxItem item,
        int date)
    {
        if (toPeerType == PeerType.Channel)
        {
            return new TUpdateShort
            {
                Date = date,
                Update = new TUpdateDeleteChannelMessages
                {
                    ChannelId = item.OwnerPeerId,
                    Messages = new TVector<int>(item.DeletedMessageIdList),
                    Pts = item.Pts,
                    PtsCount = item.PtsCount
                }
            };
        }

        return new TUpdates
        {
            Updates = new TVector<IUpdate>(new TUpdateDeleteMessages
            {
                Messages = new TVector<int>(item.DeletedMessageIdList),
                Pts = item.Pts,
                PtsCount = item.PtsCount
            }),
            Chats = [],
            Users = [],
            Date = date,
            Seq = 0
        };
    }

    public virtual IUpdates ToDraftsUpdates(IReadOnlyCollection<IDraftReadModel> draftReadModels, int layer)
    {
        var converter = draftMessageLayeredService.GetConverter(layer);
        var draftUpdates = draftReadModels.Select(p => new TUpdateDraftMessage
        {
            Draft = converter.ToDraftMessage(p),
            Peer = p.Peer.ToPeer(),
            TopMsgId = p.Draft.TopMsgId
        });

        return new TUpdates
        {
            Chats = [],
            Date = DateTime.UtcNow.ToTimestamp(),
            Users = [],
            Updates = new TVector<IUpdate>(draftUpdates)
        };
    }

    public IUpdates ToInboxForwardMessageUpdates(ReceiveInboxMessageCompletedSagaEvent aggregateEvent)
    {
        var item = aggregateEvent.MessageItem;
        return ToInboxForwardMessageUpdates(item, item.Pts);
    }

    public IUpdates ToInviteToChannelUpdates(SendOutboxMessageCompletedSagaEvent aggregateEvent,
        StartInviteToChannelEvent startInviteToChannelEvent,
        IChannelReadModel channelReadModel,
        //IChat channel,
        bool createUpdatesForSelf,
        int layer
        )
    {
        var item = aggregateEvent.MessageItem;
        //var channel = GetChatConverter().ToChannel(
        //    createUpdatesForSelf ? item.SenderPeer.PeerId : 0,
        //    channelReadModel,
        //    null,
        //    null, false);
        var channel = chatConverterService.ToChannel(createUpdatesForSelf ? item.SenderPeer.PeerId : 0,
            channelReadModel,
            null,
            null,
            false,
            aggregateEvent.RequestInfo.Layer
        );

        if (!createUpdatesForSelf)
        {
            return new TUpdates
            {
                Updates = new TVector<IUpdate>([new TUpdateChannel
                {
                    ChannelId = channel.Id
                }
                ]),
                Chats = new TVector<IChat>(channel),
                Users = [],
                Date = DateTime.UtcNow.ToTimestamp()
            };
        }

        var updateList = ToChannelMessageServiceUpdates(item.MessageId,
            item.RandomId,
            item.Pts,
            item.SenderPeer,
            item.ToPeer,
            new TMessageActionChatAddUser { Users = new TVector<long>(startInviteToChannelEvent.MemberUidList) },
            item.Date,
            0,
            createUpdatesForSelf,
            layer
        );

        return new TUpdates
        {
            Chats = new TVector<IChat>(channel),
            Date = item.Date,
            Updates = new TVector<IUpdate>(updateList),
            Users = []
        };
    }

    public IUpdates ToSelfUpdatePinnedMessageUpdates(UpdatePinnedMessageCompletedSagaEvent aggregateEvent)
    {
        return ToUpdatePinnedMessageUpdates(aggregateEvent, true);
    }

    public IUpdates ToUpdatePinnedMessageServiceUpdates(long selfUserId, SendOutboxMessageCompletedSagaEvent aggregateEvent, int layer)
    {
        var item = aggregateEvent.MessageItem;
        //var update = ToMessageServiceUpdate(item.MessageId,
        //    item.Pts,
        //    item.Post ? null : item.SenderPeer,
        //    item.ToPeer,
        //    new TMessageActionPinMessage(),
        //    item.Date,
        //    0,
        //    item.InputReplyTo);
        var update = ToMessageServiceUpdate(selfUserId, item, layer);
        return new TUpdates
        {
            Date = item.Date,
            Users = [],
            Chats = [],
            Seq = 0,
            Updates = new TVector<IUpdate>(update)
        };
    }

    public IUpdates ToUpdatePinnedMessageUpdates(UpdatePinnedMessageCompletedSagaEvent aggregateEvent)
    {
        return ToUpdatePinnedMessageUpdates(aggregateEvent, false);
    }

    public IUpdates ToUpdatePinnedMessageUpdates(SendOutboxMessageCompletedSagaEvent aggregateEvent, int layer)
    {
        var item = aggregateEvent.MessageItem;
        var updateMessageId = new TUpdateMessageID
        {
            Id = item.MessageId,
            RandomId = item.RandomId
        };

        var fromPeer = item.SendAs ?? item.SenderPeer;

        //var messageServiceUpdate = ToMessageServiceUpdate(item.MessageId,
        //    item.Pts,
        //    item.Post ? null : fromPeer,
        //    item.ToPeer,
        //    new TMessageActionPinMessage(),
        //    item.Date,
        //    aggregateEvent.RequestInfo.UserId,
        //    item.InputReplyTo);

        //TODO: sendAs

        var update = ToMessageServiceUpdate(aggregateEvent.RequestInfo.UserId, item with { SenderPeer = item.SendAs ?? item.SenderPeer }, layer);

        return new TUpdates
        {
            Date = item.Date,
            Users = [],
            Chats = [],
            Seq = 0,
            Updates = new TVector<IUpdate>(updateMessageId, update)
        };
    }

    public IUpdates ToUpdatePinnedMessageUpdates(ReceiveInboxMessageCompletedSagaEvent aggregateEvent)
    {
        var item = aggregateEvent.MessageItem;

        //var update = ToMessageServiceUpdate(item.MessageId,
        //    item.Pts,
        //    null,
        //    item.ToPeer,
        //    new TMessageActionPinMessage(),
        //    item.Date,
        //    item.OwnerPeer.PeerId,
        //    item.InputReplyTo);
        var update = ToMessageServiceUpdate(0, item, 0);
        return new TUpdates
        {
            Updates = new TVector<IUpdate>(update),
            Chats = [],
            Date = item.Date,
            Seq = 0,
            Users = []
        };
    }

    private static TChatParticipants ToChatParticipants(long chatId,
        IReadOnlyList<ChatMember> chatMemberList,
        int date,
        long creatorUid,
        int chatVersion)
    {
        var participants = chatMemberList.Select(p =>
        {
            if (p.UserId == creatorUid)
            {
                return (IChatParticipant)new TChatParticipantCreator { UserId = p.UserId };
            }

            return new TChatParticipant { Date = date, InviterId = creatorUid, UserId = p.UserId };
        }).ToList();

        return new TChatParticipants
        {
            ChatId = chatId,
            Participants = new TVector<IChatParticipant>(participants),
            Version = chatVersion
        };
    }

    private List<IUpdate> ToChannelMessageServiceUpdates(int messageId,
        long randomId,
        int pts,
        Peer? fromPeer,
        Peer toPeer,
        IMessageAction messageAction,
        int date,
        int? replyToMsgId,
        bool createUpdatesForSelf, int layer)
    {
        var updateMessageId = new TUpdateMessageID { Id = messageId, RandomId = randomId };
        //only in create channel
        //var updateChannel = new TUpdateChannel { ChannelId = peerId};

        var updateReadChannelInbox = new TUpdateReadChannelInbox
        {
            ChannelId = toPeer.PeerId,
            MaxId = messageId,
            // FolderId = 0,
            Pts = pts,
            StillUnreadCount = 0
        };
        var isOut = createUpdatesForSelf;
        var message = new TMessageService
        {
            Action = messageAction,
            Date = date,
            FromId = fromPeer.ToPeer(),
            Out = isOut,
            PeerId = toPeer.ToPeer(),
            Id = messageId,
            ReplyTo = ToMessageReplyHeader(replyToMsgId, null)
        };
        var updateNewChannelMessage = new TUpdateNewChannelMessage
        {
            Pts = pts,
            PtsCount = 1,
            Message = messageResponseService.ToLayeredData(message, layer)
        };

        return [updateMessageId, updateReadChannelInbox, updateNewChannelMessage];
    }

    private IUpdates ToEditUpdates(
        List<ReactionCount>? oldReactions,
        List<Reaction>? recentReactions,
        MessageItem item,
        int pts,
        long selfUserId = 0,
        List<UserReaction>? userReactions = null,
        long? linkedChannelId = null,
        int layer = 0
    )
    {
        var canSeeList = item.IsOut &&
                         (item.ToPeer.PeerType == PeerType.Channel || item.ToPeer.PeerType == PeerType.Chat);
        var reactions = oldReactions; // GetAllReactions(oldReactions, addedReactions, removedReactions);
        var newRecentReactions = recentReactions; // GetRecentReactions(recentReactions, addedReactions);
        //var messageReactions = reactionLayeredService.GetConverter(layer)
        //    .ToMessageReactions(selfUserId, item.ToPeer, reactions, newRecentReactions, canSeeList, userReactions);
        var message = messageConverterService
            .ToMessage(selfUserId, item, reactions, recentReactions, userReactions, layer: layer);

        IUpdate update = item.ToPeer.PeerType switch
        {
            PeerType.Channel => new TUpdateEditChannelMessage
            {
                Pts = pts,
                PtsCount = 1,
                Message = message
            },
            _ => new TUpdateEditMessage
            {
                Pts = pts,
                PtsCount = 1,
                Message = message
            }
        };

        return new TUpdates
        {
            Updates = new TVector<IUpdate>(update, new TUpdateRecentReactions()),
            Users = [],
            Chats = [],
            Date = DateTime.UtcNow.ToTimestamp(),
            Seq = 0
        };
    }

    private IUpdates ToInboxForwardMessageUpdates(MessageItem item,
        int pts)
    {
        var updateNewMessage =
            new TUpdateNewMessage
            { Message = messageConverterService.ToMessage(0, item), Pts = pts, PtsCount = 1 };
        return new TUpdates
        {
            Chats = [],
            Date = item.Date,
            Users = [],
            Seq = 0,
            Updates = new TVector<IUpdate>(updateNewMessage)
        };
    }

    private IUpdate ToMessageServiceUpdate(long selfUserId, MessageItem item, int layer)
    {
        //var isOut = false;
        //if (fromPeer != null)
        //{
        //    isOut = selfUserId == fromPeer.PeerId;
        //}

        //var m = new TMessageService
        //{
        //    Action = messageAction,
        //    Date = date,
        //    FromId = fromPeer.ToPeer(),
        //    Out = isOut,
        //    PeerId = toPeer.ToPeer(),
        //    Id = messageId,
        //    ReplyTo = inputReplyTo.ToMessageReplyHeader(),// GetMessageConverter().ToMessageReplyHeader(inputReplyTo)
        //};
        var m = messageConverterService.ToMessage(selfUserId, item, layer: layer);

        if (item.ToPeer.PeerType == PeerType.Channel)
        {
            if (item.MessageAction is TMessageActionHistoryClear)
            {
                return new TUpdateEditChannelMessage { Message = m, Pts = item.Pts, PtsCount = 1 };
            }

            return new TUpdateNewChannelMessage { Message = m, Pts = item.Pts, PtsCount = 1 };
        }

        IUpdate updateNewMessage = new TUpdateNewMessage { Pts = item.Pts, PtsCount = 1, Message = m };
        if (item.MessageAction is TMessageActionHistoryClear)
        {
            updateNewMessage = new TUpdateEditMessage { Pts = item.Pts, PtsCount = 1, Message = m };
        }

        return updateNewMessage;
    }

    private List<IUpdate> ToSelfMessageServiceUpdates(
        int messageId,
        long randomId,
        int pts,
        Peer fromPeer,
        Peer toPeer,
        IMessageAction messageAction,
        int date,
        int? replyToMsgId)
    {
        var updateMessageId = new TUpdateMessageID { Id = messageId, RandomId = randomId };
        var updateNewMessage = new TUpdateNewMessage
        {
            Pts = pts,
            PtsCount = 1,
            Message = new TMessageService
            {
                Action = messageAction,
                Date = date,
                FromId = fromPeer.ToPeer(),
                Out = true,
                PeerId = toPeer.ToPeer(),
                Id = messageId,
                ReplyTo = ToMessageReplyHeader(replyToMsgId, null)
            }
        };

        return [updateMessageId, updateNewMessage];
    }

    private IUpdates ToUpdatePinnedMessageUpdates(UpdatePinnedMessageCompletedSagaEvent aggregateEvent,
        bool createForSelf)
    {
        if (aggregateEvent.ToPeer.PeerType == PeerType.Channel)
        {
            var updatePinnedChannelMessages = new TUpdatePinnedChannelMessages
            {
                Pinned = aggregateEvent.Pinned,
                ChannelId = aggregateEvent.ToPeer.PeerId,
                Messages = new TVector<int>(aggregateEvent.MessageId),
                Pts = aggregateEvent.Pts,
                PtsCount = 1
            };
            return new TUpdateShort { Update = updatePinnedChannelMessages, Date = aggregateEvent.Date };
        }

        var toPeer = aggregateEvent.ToPeer;
        if (!createForSelf)
        {
            toPeer = new Peer(PeerType.User, aggregateEvent.SenderPeerId);
        }

        var updatePinnedMessages = new TUpdatePinnedMessages
        {
            Messages = new TVector<int>(aggregateEvent.MessageId),
            Pts = aggregateEvent.Pts,
            Peer = toPeer.ToPeer(),
            Pinned = aggregateEvent.Pinned,
            PtsCount = 1
        };
        var updates = new TUpdates
        {
            Chats = [],
            Date = aggregateEvent.Date,
            Updates = new TVector<IUpdate>(updatePinnedMessages),
            Seq = 0,
            Users = []
        };
        return updates;
    }

    public IMessageReplyHeader? ToMessageReplyHeader(int? replyToMsgId,
        int? topMsgId)
    {
        if (replyToMsgId > 0)
        {
            return new TMessageReplyHeader
            {
                ReplyToMsgId = replyToMsgId.Value,
                ReplyToTopId = topMsgId /*, ForumTopic = topMsgId.HasValue*/
            };
        }

        return null;
    }
}