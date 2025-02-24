namespace MyTelegram.Messenger.TLObjectConverters.Impl.LatestLayer;

public class UpdatesConverterLatest(
    ILayeredService<IChatConverter> layeredChatService,
    ILayeredService<IMessageConverter> layeredMessageService)
    : LayeredConverterBase, IUpdatesConverterLatest
{
    private IChatConverter? _chatConverter;
    private IMessageConverter? _messageConverter;

    public override int Layer => Layers.LayerLatest;

    public IUpdates ToChannelMessageUpdates(SendOutboxMessageCompletedSagaEvent aggregateEvent, bool mentioned = false)
    {
        if (aggregateEvent.IsSendGroupedMessages)
        {
            return CreateSendGroupedChannelMessageUpdates(aggregateEvent);
        }

        var item = aggregateEvent.MessageItems.First();
        // selfUser==-1 means the updates is for channel member except sender
        const int selfUserId = -1;
        var updateNewChannelMessage =
            new TUpdateNewChannelMessage
            {
                Message = GetMessageConverter().ToMessage(item,
                    selfUserId,
                    mentioned: mentioned),
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

    private IUpdates CreateSendGroupedChannelMessageUpdates(SendOutboxMessageCompletedSagaEvent aggregateEvent)
    {
        List<IUpdate> updateList = aggregateEvent.MessageItems.Select(p => (IUpdate)new TUpdateMessageID
        {
            Id = p.MessageId,
            RandomId = p.RandomId,
        }).ToList();

        // selfUser==-1 means the updates is for channel member except sender
        const int selfUserId = -1;
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
                Message = GetMessageConverter().ToMessage(item, selfUserId),
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
        IPhotoReadModel? photoReadModel)
    {
        var channel = GetChatConverter().ToChannel(selfUserId, channelReadModel, photoReadModel, null, false);

        return new TUpdates
        {
            Chats = new TVector<IChat>(channel),
            Updates = new TVector<IUpdate>(new TUpdateChannel { ChannelId = channelReadModel.ChannelId }),
            Users = new TVector<IUser>(),
            Date = DateTime.UtcNow.ToTimestamp(),
            Seq = 0
        };
    }

    public IUpdates ToCreateChannelUpdates(ChannelCreatedEvent eventData,
        SendOutboxMessageCompletedSagaEvent aggregateEvent)
    {
        var item = aggregateEvent.MessageItem;
        var updateList = ToChannelMessageServiceUpdates(item.MessageId,
            eventData.RandomId,
            item.Pts,
            null,
            item.OwnerPeer with { PeerType = PeerType.Channel },
            new TMessageActionChannelCreate { Title = eventData.Title },
            item.Date,
            0);
        var updateChannel = new TUpdateChannel { ChannelId = eventData.ChannelId };
        updateList.Insert(1, updateChannel);
        var channel = GetChatConverter().ToChannel(eventData);
        channel.Creator = true;

        var updates = new TUpdates
        {
            Chats = new TVector<IChat>(channel),
            Date = item.Date,
            Updates = new TVector<IUpdate>(updateList),
            Users = []
        };
        return updates;
    }

    public IUpdates ToCreateChatUpdates(ChatCreatedEvent eventData,
        SendOutboxMessageCompletedSagaEvent aggregateEvent, IChatReadModel chatReadModel)
    {
        var item = aggregateEvent.MessageItem;

        var updates = ToSelfMessageServiceUpdates(item.MessageId,
            eventData.RandomId,
            item.Pts,
            new Peer(PeerType.User, eventData.CreatorUid),
            new Peer(PeerType.Chat, eventData.ChatId),
            new TMessageActionChatCreate
            {
                Title = chatReadModel.Title,
                Users = new TVector<long>(eventData.MemberUidList.Select(p => p.UserId).ToList())
            },
            eventData.Date,
            0
        );
        var chatParticipant = ToChatParticipants(eventData.ChatId,
            eventData.MemberUidList,
            eventData.Date,
            eventData.CreatorUid,
            0);
        var updateChatParticipants = new TUpdateChatParticipants { Participants = chatParticipant };
        var chat = GetChatConverter().ToChat(aggregateEvent.RequestInfo.UserId, chatReadModel, null);
        updates.Add(updateChatParticipants);

        return new TUpdates
        {
            Chats = new TVector<IChat>(chat),
            Date = eventData.Date,
            Seq = 0,
            Updates = new TVector<IUpdate>(updates),
            Users = []
        };
    }

    public IUpdates ToCreateChatUpdates(ChatCreatedEvent eventData,
        ReceiveInboxMessageCompletedSagaEvent aggregateEvent, IChatReadModel chatReadModel)
    {
        var item = aggregateEvent.MessageItem;

        var update = ToMessageServiceUpdate(item.MessageId,
            //eventData.RandomId,
            item.Pts,
            item.SenderPeer with { PeerType = PeerType.User },
            item.ToPeer,
            new TMessageActionChatCreate
            {
                Title = chatReadModel.Title,
                Users = new TVector<long>(eventData.MemberUidList.Select(p => p.UserId).ToList())
            },
            eventData.Date,
            0,
            null
        );
        var chat = GetChatConverter().ToChat(item.OwnerPeer.PeerId, chatReadModel, null);
        var updates = new TUpdates
        {
            Chats = new TVector<IChat>(chat),
            Date = item.Date,
            Users = [],
            Updates = new TVector<IUpdate>(update)
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
            Chats = new TVector<IChat>(),
            Users = new TVector<IUser>(),
            Date = date,
            Seq = 0
        };
    }

    public virtual IUpdates ToDraftsUpdates(IReadOnlyCollection<IDraftReadModel> draftReadModels)
    {
        var draftUpdates = draftReadModels.Select(p => new TUpdateDraftMessage
        {
            Draft = new TDraftMessage
            {
                InvertMedia = p.Draft.InvertMedia,
                Effect = p.Draft.Effect,
                Date = p.Draft.Date,
                Message = p.Draft.Message,
                Entities = p.Draft.Entities.ToTObject<TVector<IMessageEntity>>(),
                NoWebpage = p.Draft.NoWebpage,
                ReplyTo = new TInputReplyToMessage
                {
                    ReplyToMsgId = p.Draft.ReplyToMsgId ?? 0
                }
                //ReplyToMsgId = p.Draft.ReplyToMsgId
            },
            Peer = p.Peer.ToPeer(),
            TopMsgId = p.Draft.TopMsgId
        });

        return new TUpdates
        {
            Chats = new TVector<IChat>(),
            Date = DateTime.UtcNow.ToTimestamp(),
            Users = new TVector<IUser>(),
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
        bool createUpdatesForSelf)
    {
        var item = aggregateEvent.MessageItems.First();
        var channel = GetChatConverter().ToChannel(
            createUpdatesForSelf ? item.SenderPeer.PeerId : 0,
            channelReadModel,
            null,
            null, false);
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
            createUpdatesForSelf
        );

        return new TUpdates
        {
            Chats = new TVector<IChat>(channel),
            Date = item.Date,
            Updates = new TVector<IUpdate>(updateList),
            Users = []
        };
    }


    public IUpdates ToInviteToChannelUpdates(
        //IChannelReadModel channelReadModel,
        IChat channel,
        IUserReadModel senderUserReadModel,
        int date)
    {
        var update = new TUpdateChannel { ChannelId = channel.Id };
        return new TUpdates
        {
            Chats = new TVector<IChat>(channel),
            Users = new TVector<IUser>(),
            Date = date,
            Updates = new TVector<IUpdate>(update)
        };
    }

    public IUpdates ToReadHistoryUpdates(ReadHistoryCompletedSagaEvent eventData)
    {
        var peer = eventData.ReaderToPeer.PeerType == PeerType.User
            ? new TPeerUser { UserId = eventData.ReaderUserId }
            : eventData.ReaderToPeer.ToPeer();
        var updateReadHistoryOutbox = new TUpdateReadHistoryOutbox
        {
            Pts = eventData.SenderPts,
            MaxId = eventData.SenderMessageId,
            PtsCount = 1,
            Peer = peer
        };
        var updates = new TUpdates
        {
            Chats = new TVector<IChat>(),
            Date = DateTime.UtcNow.ToTimestamp(),
            Updates = new TVector<IUpdate>(updateReadHistoryOutbox),
            Users = new TVector<IUser>(),
            Seq = 0
        };
        return updates;
    }

    public IUpdates ToReadHistoryUpdates(UpdateOutboxMaxIdCompletedSagaEvent eventData)
    {
        var updateReadHistoryOutbox = new TUpdateReadHistoryOutbox
        {
            Pts = eventData.Pts,
            MaxId = eventData.MaxId,
            PtsCount = 1,
            Peer = eventData.ToPeerId.ToUserPeer().ToPeer()
        };

        var updates = new TUpdates
        {
            Chats = new TVector<IChat>(),
            Date = DateTime.UtcNow.ToTimestamp(),
            Updates = new TVector<IUpdate>(updateReadHistoryOutbox),
            Users = new TVector<IUser>(),
            Seq = 0
        };

        return updates;
    }

    public IUpdates ToSelfUpdatePinnedMessageUpdates(UpdatePinnedMessageCompletedSagaEvent aggregateEvent)
    {
        return ToUpdatePinnedMessageUpdates(aggregateEvent, true);
    }

    public IUpdates ToUpdatePinnedMessageServiceUpdates(SendOutboxMessageCompletedSagaEvent aggregateEvent)
    {
        var item = aggregateEvent.MessageItem;
        var update = ToMessageServiceUpdate(item.MessageId,
            item.Pts,
            item.Post ? null : item.SenderPeer,
            item.ToPeer,
            new TMessageActionPinMessage(),
            item.Date,
            0,
            item.InputReplyTo);
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

    public IUpdates ToUpdatePinnedMessageUpdates(SendOutboxMessageCompletedSagaEvent aggregateEvent)
    {
        var item = aggregateEvent.MessageItem;
        var updateMessageId = new TUpdateMessageID
        {
            Id = item.MessageId,
            RandomId = item.RandomId
        };

        //var updatePinnedMessages = new TUpdatePinnedMessages
        //{
        //    Pinned = true,
        //    Peer = item.ToPeer.ToPeer(),
        //    Messages = new TVector<int> { item.InputReplyTo.ToReplyToMsgId() ?? 0 },
        //    Pts = aggregateEvent.Pts,
        //    PtsCount = 1
        //};
        var fromPeer = item.SendAs ?? item.SenderPeer;

        var messageServiceUpdate = ToMessageServiceUpdate(item.MessageId,
            item.Pts,
            item.Post ? null : fromPeer,
            item.ToPeer,
            new TMessageActionPinMessage(),
            item.Date,
            aggregateEvent.RequestInfo.UserId,
            item.InputReplyTo);
        return new TUpdates
        {
            Date = item.Date,
            Users = [],
            Chats = [],
            Seq = 0,
            Updates = new TVector<IUpdate>(updateMessageId, messageServiceUpdate)
        };
    }

    public IUpdates ToUpdatePinnedMessageUpdates(ReceiveInboxMessageCompletedSagaEvent aggregateEvent)
    {
        var item = aggregateEvent.MessageItem;

        var update = ToMessageServiceUpdate(item.MessageId,
            item.Pts,
            null,
            item.ToPeer,
            new TMessageActionPinMessage(),
            item.Date,
            item.OwnerPeer.PeerId,
            item.InputReplyTo);
        return new TUpdates
        {
            Updates = new TVector<IUpdate>(update),
            Chats = [],
            Date = item.Date,
            Seq = 0,
            Users = []
        };
    }

    protected virtual IChatConverter GetChatConverter()
    {
        return _chatConverter ??= layeredChatService.GetConverter(GetLayer());
    }

    protected virtual IMessageConverter GetMessageConverter()
    {
        return _messageConverter ??= layeredMessageService.GetConverter(GetLayer());
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
        bool createUpdatesForSelf = true)
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
        var updateNewChannelMessage = new TUpdateNewChannelMessage
        {
            Pts = pts,
            PtsCount = 1,
            Message = new TMessageService
            {
                Action = messageAction,
                Date = date,
                FromId = fromPeer.ToPeer(),
                Out = isOut,
                PeerId = toPeer.ToPeer(),
                Id = messageId,
                ReplyTo = GetMessageConverter().ToMessageReplyHeader(replyToMsgId, null)
            }
        };

        return new List<IUpdate> { updateMessageId, updateReadChannelInbox, updateNewChannelMessage };
    }

    private IUpdates ToInboxForwardMessageUpdates(MessageItem aggregateEvent,
        int pts)
    {
        var updateNewMessage =
            new TUpdateNewMessage
            { Message = GetMessageConverter().ToMessage(aggregateEvent), Pts = pts, PtsCount = 1 };
        return new TUpdates
        {
            Chats = new TVector<IChat>(),
            Date = aggregateEvent.Date,
            Users = new TVector<IUser>(),
            Seq = 0,
            Updates = new TVector<IUpdate>(updateNewMessage)
        };
    }

    private IUpdate ToMessageServiceUpdate(int messageId,
        //long randomId,
        int pts,
        Peer? fromPeer,
        Peer toPeer,
        IMessageAction messageAction,
        int date,
        long selfUserId,
        IInputReplyTo? inputReplyTo)
    {
        var isOut = false;
        if (fromPeer != null)
        {
            isOut = selfUserId == fromPeer.PeerId;
        }

        var m = new TMessageService
        {
            Action = messageAction,
            Date = date,
            FromId = fromPeer.ToPeer(),
            Out = isOut,
            PeerId = toPeer.ToPeer(),
            Id = messageId,
            ReplyTo = GetMessageConverter().ToMessageReplyHeader(inputReplyTo)
        };

        if (toPeer.PeerType == PeerType.Channel)
        {
            if (messageAction is TMessageActionHistoryClear)
            {
                return new TUpdateEditChannelMessage { Message = m, Pts = pts, PtsCount = 1 };
            }

            return new TUpdateNewChannelMessage { Message = m, Pts = pts, PtsCount = 1 };
        }

        IUpdate updateNewMessage = new TUpdateNewMessage { Pts = pts, PtsCount = 1, Message = m };
        if (messageAction is TMessageActionHistoryClear)
        {
            updateNewMessage = new TUpdateEditMessage { Pts = pts, PtsCount = 1, Message = m };
        }

        return updateNewMessage;
    }
    
    private List<IUpdate> ToSelfMessageServiceUpdates(int messageId,
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
                ReplyTo = GetMessageConverter().ToMessageReplyHeader(replyToMsgId, null)
            }
        };

        return new List<IUpdate> { updateMessageId, updateNewMessage };
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
            Chats = new TVector<IChat>(),
            Date = aggregateEvent.Date,
            Updates = new TVector<IUpdate>(updatePinnedMessages),
            Seq = 0,
            Users = new TVector<IUser>()
        };
        return updates;
    }
}