namespace MyTelegram.Messenger.TLObjectConverters.Impl.LatestLayer;

public class MessageConverterLatest(
    IPeerHelper peerHelper,
    ILayeredService<IPollConverter> layeredPollService)
    : LayeredConverterBase, IMessageConverterLatest
{
    private IPollConverter? _pollConverter;

    public override int Layer => Layers.LayerLatest;

    public IMessage ToDiscussionMessage(long selfUserId,
        IMessageReadModel messageReadModel /*, IReplyReadModel? replyReadModel*/)
    {
        var m = ToMessage(messageReadModel, null, null, selfUserId);
        if (m is TMessage tMessage)
        {
            var replies = ToMessageReplies(messageReadModel.Post, messageReadModel.Reply);
            tMessage.Replies = replies;
        }

        return m;
    }

    public virtual IMessage ToMessage(MessageItem item,
        long selfUserId = 0,
        long? linkedChannelId = null,
        int pts = 0,
        List<ReactionCount>? reactions = null,
        List<Reaction>? recentReactions = null,
        //int? editDate = null,
        //bool editHide = false,
        List<UserReaction>? userReactions = null,
        bool mentioned = false
    )
    {
        var isOut = item.IsOut;
        var canSeeList = item.IsOut &&
                         (item.ToPeer.PeerType == PeerType.Channel || item.ToPeer.PeerType == PeerType.Chat);

        if (item.ToPeer.PeerType == PeerType.Channel && selfUserId != 0)
        {
            isOut = item.SenderPeer.PeerId == selfUserId;
        }

        switch (item.SendMessageType)
        {
            case SendMessageType.MessageService:
                {
                    if (string.IsNullOrEmpty(item.MessageActionData))
                    {
                        throw new ArgumentNullException(nameof(item),
                            "MessageActionData can not be null for service message");
                    }

                    var bytes = item.MessageActionData.ToBytes();
                    var fromId = item.SenderPeer.ToPeer();
                    //if (box.ToPeer.PeerType == PeerType.Channel && box.Post)
                    //{
                    //    fromId = null;
                    //}

                    if (item.ToPeer.PeerType == PeerType.Channel)
                    {
                        if (item.Post || item.MessageSubType == MessageSubType.CreateChannel)
                        {
                            fromId = null;
                        }
                    }

                    var m = new TMessageService
                    {
                        Date = item.Date,
                        //Silent = outbox.Silent,
                        Post = false, // outbox.Post,
                        PeerId = item.ToPeer.ToPeer(),
                        FromId = fromId,
                        Id = item.MessageId,
                        //Out = item.IsOut,
                        Out = isOut,
                        Action = bytes.ToTObject<IMessageAction>(),
                        ReplyTo = item.InputReplyTo.ToMessageReplyHeader(),
                        Mentioned = mentioned,
                        MediaUnread = mentioned,
                    };

                    return m;
                }
            //break;

            default:
                {
                    var m = new TMessage
                    {
                        Date = item.Date,
                        EditDate = item.EditDate,
                        EditHide = item.EditHide,
                        Entities = item.Entities,
                        FromId = item.SenderPeer.ToPeer(),
                        PeerId = item.ToPeer.ToPeer(),
                        Id = item.MessageId,
                        Message = item.Message,
                        Out = isOut,
                        FwdFrom = ToMessageFwdHeader(item.FwdHeader),
                        GroupedId = item.GroupId,
                        Media = item.Media,
                        Views = item.Views,
                        Forwards = item.Views.HasValue ? 0 : null,
                        Post = item.Post,
                        Mentioned = mentioned,
                        MediaUnread = mentioned,
                        PostAuthor = item.PostAuthor,
                        SavedPeerId = item.SavedPeerId.ToPeer(),
                        Effect = item.Effect,
                        Pinned = item.Pinned,
                        InvertMedia = item.InvertMedia
                    };
                    if (item.ToPeer.PeerType == PeerType.Channel)
                    {
                        //m.FromId = null;
                        if (item.Post /*|| item.SenderPeer.PeerId == selfUserId*/)
                        {
                            m.FromId = null;
                        }
                        else
                        {
                            if (item.SendAs != null)
                            {
                                m.FromId = item.SendAs.ToPeer();
                            }
                        }

                        m.Replies = ToMessageReplies(item.Post, item.Reply);
                        if (m.Replies != null && item.FwdHeader?.SavedFromPeer != null) // forward from linked channel
                        {
                            //m.FromId = _peerHelper.ToPeer(PeerType.Channel, item.FwdHeader.SavedFromPeer.PeerId);
                            m.FromId = item.FwdHeader.SavedFromPeer.ToPeer();
                            m.Out = false;
                        }
                    }

                    // Process existing data
                    if (item.EditDate == 0)
                    {
                        m.EditDate = null;
                    }

                    m.ReplyTo = item.InputReplyTo.ToMessageReplyHeader();
                    m.ReplyMarkup = item.ReplyMarkup;

                    //m.Reactions = GetReactionConverter().ToMessageReactions(selfUserId,
                    //    item.ToPeer,
                    //    reactions,
                    //    recentReactions,
                    //    canSeeList,
                    //    userReactions);

                    return m;
                }
                //break;
        }
    }

    public IMessage ToMessage(InboxMessageEditCompletedSagaEvent aggregateEvent)
    {
        var item = aggregateEvent.NewMessageItem;
        return ToMessage(item, reactions: item.Reactions, recentReactions: item.RecentReactions);
    }

    public IMessage ToMessage(OutboxMessageEditCompletedSagaEvent aggregateEvent,
        long selfUserId)
    {
        var item = aggregateEvent.NewMessageItem;
        var m = ToMessage(item, selfUserId, reactions: item.Reactions, recentReactions: item.RecentReactions);

        return m;
    }

    public IMessage ToMessage(IMessageReadModel readModel,
        IPollReadModel? pollReadModel,
        List<string>? chosenOptions,
        long selfUserId)
    {
        switch (readModel.SendMessageType)
        {
            case SendMessageType.MessageService:
                {
                    if (string.IsNullOrEmpty(readModel.MessageActionData) && readModel.MessageAction == null)
                    {
                        ArgumentNullException.ThrowIfNull(readModel.MessageActionData);
                    }

                    var bytes = readModel.MessageActionData?.ToBytes();
                    var fromId = peerHelper.ToPeer(PeerType.User, readModel.SenderPeerId);

                    if (readModel.ToPeerType == PeerType.Channel)
                    {
                        if (readModel.Post || readModel.MessageActionType == MessageActionType.ChannelCreate ||
                            readModel.MessageActionType == MessageActionType.SetMessagesTtl)
                        {
                            fromId = null;
                        }
                    }

                    if (readModel.SendAs != null)
                    {
                        fromId = readModel.SendAs.ToPeer();
                    }

                    var m = new TMessageService
                    {
                        Date = readModel.Date,
                        Silent = readModel.Silent,
                        Post = readModel.Post,
                        PeerId = peerHelper.ToPeer(readModel.ToPeerType, readModel.ToPeerId),
                        FromId = fromId,
                        Id = readModel.MessageId,
                        Out = readModel.SenderPeerId == selfUserId,
                        Action = readModel.MessageAction ?? bytes.ToTObject<IMessageAction>(),
                        ReplyTo = ToMessageReplyHeader(readModel.ReplyTo),
                        TtlPeriod = readModel.TtlPeriod,
                    };

                    return m;
                }
            default:
                {
                    var fromId = peerHelper.ToPeer(PeerType.User, readModel.SenderPeerId);
                    var toPeerId = peerHelper.ToPeer(readModel.ToPeerType, readModel.ToPeerId);

                    var m = new TMessage
                    {
                        Date = readModel.Date,
                        EditDate = readModel.EditDate,
                        EditHide = readModel.EditHide,
                        Message = readModel.Message,
                        Silent = readModel.Silent,
                        Post = readModel.Post,
                        PostAuthor = readModel.PostAuthor,
                        GroupedId = readModel.GroupedId,
                        Views = readModel.Views,
                        Forwards = readModel.Views.HasValue ? 0 : null,
                        Entities = readModel.Entities2 ?? readModel.Entities.ToTObject<TVector<IMessageEntity>>(),
                        PeerId = toPeerId,
                        FromId = fromId,
                        Id = readModel.MessageId,
                        Out = readModel.SenderPeerId == selfUserId,
                        Pinned = readModel.Pinned,
                        FwdFrom = ToMessageFwdHeader(readModel.FwdHeader),
                        Media = readModel.Media2 ?? readModel.Media.ToTObject<IMessageMedia>(),
                        ReplyTo = ToMessageReplyHeader(readModel.ReplyTo),
                        ReplyMarkup = readModel.ReplyMarkup.ToTObject<IReplyMarkup>(),
                        SavedPeerId = readModel.SavedPeerId.ToPeer(),
                        Effect = readModel.Effect,
                        InvertMedia = readModel.InvertMedia
                    };
                    if (m.GroupedId == 0)
                    {
                        m.GroupedId = null;
                    }

                    // Process existing data
                    if (readModel.EditDate == 0)
                    {
                        m.EditDate = null;
                    }

                    if (pollReadModel != null)
                    {
                        m.Media = new TMessageMediaPoll
                        {
                            Poll = GetPollConverter().ToPoll(pollReadModel),
                            Results = GetPollConverter().ToPollResults(pollReadModel, chosenOptions ?? new List<string>())
                        };
                    }

                    if (readModel.ToPeerType == PeerType.Channel)
                    {
                        m.Replies = ToMessageReplies(readModel.Post, readModel.Reply);
                        if (readModel.Post)
                        {
                            m.FromId = null;
                        }
                        else
                        {
                            if (readModel.SendAs != null)
                            {
                                m.FromId = readModel.SendAs.ToPeer();
                            }
                        }

                        //m.Replies = ToMessageReplies(readModel.Post, readModel.LinkedChannelId, readModel.Pts);
                        if (m.Replies != null && readModel.FwdHeader != null) // forward from linked channel
                        {
                            //m.FromId = _peerHelper.ToPeer(PeerType.Channel, readModel.LinkedChannelId!.Value);
                            m.FromId = readModel.FwdHeader.FromId.ToPeer();
                            m.Out = false;
                        }
                    }

                    return m;
                }
        }
    }

    public IMessageFwdHeader? ToMessageFwdHeader(MessageFwdHeader? fwdHeader)
    {
        if (fwdHeader == null)
        {
            return null;
        }

        return new TMessageFwdHeader
        {
            ChannelPost = fwdHeader.ChannelPost,
            Date = fwdHeader.Date,
            FromId = fwdHeader.FromId.ToPeer(),
            FromName = fwdHeader.FromName,
            PostAuthor = fwdHeader.PostAuthor,
            SavedFromPeer = fwdHeader.SavedFromPeer.ToPeer(),
            SavedFromMsgId = fwdHeader.SavedFromMsgId
        };
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

    public IMessageReplyHeader? ToMessageReplyHeader(IInputReplyTo? inputReplyTo)
    {
        return inputReplyTo.ToMessageReplyHeader();
    }

    public IList<IMessage> ToMessages(IReadOnlyCollection<IMessageReadModel> readModels,
        IReadOnlyCollection<IPollReadModel>? pollReadModels,
        IReadOnlyCollection<IPollAnswerVoterReadModel>? pollAnswerVoterReadModels,
        long selfUserId)
    {
        var messages = new List<IMessage>();
        foreach (var readModel in readModels)
        {
            IPollReadModel? poll = null;
            List<string>? chosenOptions = null;
            if (readModel.PollId.HasValue)
            {
                poll = pollReadModels?.FirstOrDefault(p => p.PollId == readModel.PollId);
                chosenOptions = pollAnswerVoterReadModels?.Where(p => p.PollId == readModel.PollId)
                    .Select(p => p.Option).ToList();
            }

            messages.Add(ToMessage(readModel, poll, chosenOptions, selfUserId));
        }

        return messages;
    }

    //public IMessage ToDiscussionMessage(IMessageReadModel readModel,long selfUserId,long from)
    protected IPollConverter GetPollConverter()
    {
        return _pollConverter ??= layeredPollService.GetConverter(GetLayer());
    }

    private IMessageReplies? ToMessageReplies(bool post /*, long? channelId,*/, MessageReply? reply)
    {
        if (reply == null)
        {
            return null;
        }

        if (post)
        {
            if (reply.ChannelId == null)
            {
                return null;
            }
        }

        var messageReplies = new TMessageReplies
        {
            Comments = post,
            MaxId = reply.MaxId,
            Replies = reply.Replies,
            RepliesPts = reply.RepliesPts
        };

        if (post)
        {
            messageReplies.ChannelId = reply.ChannelId;
            messageReplies.RecentRepliers = new TVector<IPeer>();
            if (reply.RecentRepliers?.Count > 0)
            {
                messageReplies.RecentRepliers = new TVector<IPeer>(reply.RecentRepliers.Select(p => p.ToPeer()));
            }
        }

        return messageReplies;
    }
}