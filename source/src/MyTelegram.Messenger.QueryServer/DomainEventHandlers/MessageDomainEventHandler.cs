using MyTelegram.Handlers.Messages;
using MyTelegram.Messenger.Services.Caching;
using MyTelegram.Messenger.Services.Interfaces;
using MyTelegram.Messenger.TLObjectConverters.Interfaces;
using MyTelegram.Services.TLObjectConverters;

namespace MyTelegram.Messenger.QueryServer.DomainEventHandlers;

public partial class MessageDomainEventHandler(
    IObjectMessageSender objectMessageSender,
    ICommandBus commandBus,
    IIdGenerator idGenerator,
    IAckCacheService ackCacheService,
    IResponseCacheAppService responseCacheAppService,
    IChatEventCacheHelper chatEventCacheHelper,
    ILogger<MessageDomainEventHandler> logger,
    IQueryProcessor queryProcessor,
    ILayeredService<IUpdatesConverter> updatesLayeredService,
    ILayeredService<IChatConverter> chatLayeredService,
    ILayeredService<IUserConverter> userLayeredService,
    ILayeredService<IMessageConverter> messageLayeredService,
    IPrivacyAppService privacyAppService,
    IUserAppService userAppService,
    IChannelAppService channelAppService,
    ISendMessageDataConverter sendMessageDataConverter,
    IEditMessageDataConverter editMessageDataConverter,
    IPhotoAppService photoAppService)
    : DomainEventHandlerBase(objectMessageSender,
            commandBus,
            idGenerator,
            ackCacheService,
            responseCacheAppService),
        ISubscribeSynchronousTo<EditMessageSaga, EditMessageSagaId, OutboxMessageEditCompletedSagaEvent>,
        ISubscribeSynchronousTo<EditMessageSaga, EditMessageSagaId, InboxMessageEditCompletedSagaEvent>,
        ISubscribeSynchronousTo<MessageAggregate, MessageId, ChannelMessagePinnedEvent>,
        ISubscribeSynchronousTo<MessageAggregate, MessageId, MessageReplyUpdatedEvent>,
        ISubscribeSynchronousTo<SendMessageSaga, SendMessageSagaId, SendOutboxMessageCompletedSagaEvent>,
        ISubscribeSynchronousTo<SendMessageSaga, SendMessageSagaId, ReceiveInboxMessageCompletedSagaEvent>
{
    public Task HandleAsync(
        IDomainEvent<EditMessageSaga, EditMessageSagaId, InboxMessageEditCompletedSagaEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        var toPeer = domainEvent.AggregateEvent.NewMessageItem.OwnerPeer;
        var updates = editMessageDataConverter.Convert(domainEvent.AggregateEvent);
        //var layeredData = updatesLayeredService.GetLayeredData(c => c.ToEditUpdates(domainEvent.AggregateEvent));
        return PushUpdatesToPeerAsync(toPeer,
            updates,
            pts: domainEvent.AggregateEvent.NewMessageItem.Pts,
            updatesType: UpdatesType.Updates
        //layeredData: layeredData
        );
    }

    public async Task HandleAsync(
        IDomainEvent<EditMessageSaga, EditMessageSagaId, OutboxMessageEditCompletedSagaEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        var updates = editMessageDataConverter.Convert(domainEvent.AggregateEvent);

        await SendRpcMessageToClientAsync(domainEvent.AggregateEvent.RequestInfo,
            updates,
            domainEvent.AggregateEvent.NewMessageItem.SenderPeer.PeerId,
            domainEvent.AggregateEvent.NewMessageItem.Pts,
            domainEvent.AggregateEvent.NewMessageItem.ToPeer.PeerType
        );
        await PushUpdatesToPeerAsync(
            domainEvent.AggregateEvent.NewMessageItem.SenderPeer with { PeerType = PeerType.User },
            updates,
            domainEvent.AggregateEvent.RequestInfo.AuthKeyId,
            pts: domainEvent.AggregateEvent.NewMessageItem.Pts,
            updatesType: UpdatesType.Updates);

        // Channel message shares the same message,edit out message should notify channel member
        if (domainEvent.AggregateEvent.NewMessageItem.ToPeer.PeerType == PeerType.Channel)
        {
            var channelEditUpdates = editMessageDataConverter.ToEditMessageUpdates(domainEvent.AggregateEvent, -1);

            await PushUpdatesToPeerAsync(domainEvent.AggregateEvent.NewMessageItem.ToPeer,
                channelEditUpdates,
                pts: domainEvent.AggregateEvent.NewMessageItem.Pts);
        }
    }

    public Task HandleAsync(IDomainEvent<MessageAggregate, MessageId, ChannelMessagePinnedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task HandleAsync(IDomainEvent<MessageAggregate, MessageId, MessageReplyUpdatedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        var messageReadModel = await queryProcessor.ProcessAsync(new GetMessageByIdQuery(MessageId
                .Create(domainEvent.AggregateEvent.OwnerChannelId, domainEvent.AggregateEvent.MessageId).Value),
            cancellationToken);

        if (messageReadModel != null)
        {
            var message = messageLayeredService.Converter.ToMessage(messageReadModel, null, null, 0);
            if (message is TMessage tMessage)
            {
                tMessage.EditDate = DateTime.UtcNow.ToTimestamp();
                tMessage.EditHide = true;
            }

            var update = new TUpdateEditChannelMessage
            {
                Message = message,
                Pts = domainEvent.AggregateEvent.Pts,
                PtsCount = 1
            };
            var updates = new TUpdates
            {
                Updates = new TVector<IUpdate>(update),
                Users = new TVector<IUser>(),
                Chats = new TVector<IChat>(),
                Date = DateTime.UtcNow.ToTimestamp()
            };

            await PushUpdatesToPeerAsync(domainEvent.AggregateEvent.OwnerChannelId.ToChannelPeer(), updates);
        }
    }

    private async Task<(IChannelReadModel channelReadModel, IPhotoReadModel? photoReadModel)> GetChannelAsync(
        long channelId)
    {
        var channelReadModel = await channelAppService.GetAsync(channelId);
        var photoReadModel = await photoAppService.GetAsync(channelReadModel!.PhotoId);

        return (channelReadModel, photoReadModel);
    }

    private async Task<IUser> GetUserAsync(long userId, long selfUserId)
    {
        var userReadModel = await userAppService.GetAsync(userId);
        var photos = await photoAppService.GetPhotosAsync(userReadModel);
        var privacyList = await privacyAppService.GetPrivacyListAsync(userId);
        return userLayeredService.Converter.ToUser(selfUserId, userReadModel!, photos, privacies: privacyList);
    }

    private async Task HandleCreateChannelAsync(SendOutboxMessageCompletedSagaEvent aggregateEvent)
    {
        var item = aggregateEvent.MessageItem;
        if (chatEventCacheHelper.TryRemoveChannelCreatedEvent(item.ToPeer.PeerId,
                out var eventData))
        {
            var channelId = eventData.ChannelId;
            var updates = updatesLayeredService.GetConverter(aggregateEvent.RequestInfo.Layer)
                .ToCreateChannelUpdates(eventData, aggregateEvent);
            var layeredData =
                updatesLayeredService.GetLayeredData(c => c.ToCreateChannelUpdates(eventData, aggregateEvent));

            IObject rpcData = updates;

            if (item.MessageSubType == MessageSubType.AutoCreateChannelFromChat)
            {
                var invitedUsers = new TInvitedUsers
                {
                    Updates = updates,
                    MissingInvitees = new TVector<IMissingInvitee>()
                };

                rpcData = invitedUsers;
            }

            await SendRpcMessageToClientAsync(aggregateEvent.RequestInfo,
                rpcData,
                item.SenderPeer.PeerId
            );

            await PushUpdatesToChannelSingleMemberAsync(channelId, item.SenderPeer,
                updates,
                aggregateEvent.RequestInfo.AuthKeyId,
                pts: item.Pts,
                layeredData: layeredData
            );
        }
        else
        {
            logger.LogWarning("Cannot find cached channel info, channelId: {ChannelId}",
                item.ToPeer.PeerId);
        }
    }

    private async Task HandleCreateChatAsync(SendOutboxMessageCompletedSagaEvent aggregateEvent)
    {
        var item = aggregateEvent.MessageItem;
        if (chatEventCacheHelper.TryGetChatCreatedEvent(item.ToPeer.PeerId, out var eventData))
        {
            var chat = await queryProcessor.ProcessAsync(
                new GetChatByChatIdQuery(item.ToPeer.PeerId));
            var updates = updatesLayeredService.GetConverter(aggregateEvent.RequestInfo.Layer)
                .ToCreateChatUpdates(eventData, aggregateEvent, chat!);
            var invitedUsers = new TInvitedUsers
            {
                Updates = updates,
                MissingInvitees = new TVector<IMissingInvitee>()
            };
            var layeredData =
                updatesLayeredService.GetLayeredData(c => c.ToCreateChatUpdates(eventData, aggregateEvent, chat!));
            await SendRpcMessageToClientAsync(aggregateEvent.RequestInfo,
                invitedUsers,
                pts: item.Pts);
            await PushUpdatesToPeerAsync(item.SenderPeer,
                updates,
                aggregateEvent.RequestInfo.AuthKeyId,
                pts: item.Pts,
                layeredData: layeredData
            );
        }
        else
        {
            logger.LogWarning("Cannot find cached chat info, toPeer: {ToPeer}", item.ToPeer);
        }
    }

    private async Task HandleCreateChatAsync(ReceiveInboxMessageCompletedSagaEvent aggregateEvent)
    {
        var item = aggregateEvent.MessageItem;
        if (chatEventCacheHelper.TryGetChatCreatedEvent(item.ToPeer.PeerId, out var eventData))
        {
            var chat = await queryProcessor.ProcessAsync(
                new GetChatByChatIdQuery(item.ToPeer.PeerId));

            var updates = updatesLayeredService.Converter.ToCreateChatUpdates(eventData, aggregateEvent, chat!);
            var layeredData =
                updatesLayeredService.GetLayeredData(c => c.ToCreateChatUpdates(eventData, aggregateEvent, chat!));
            await PushUpdatesToPeerAsync(item.OwnerPeer,
                updates,
                pts: item.Pts,
                layeredData: layeredData
            );
        }
    }

    private async Task HandleForwardMessageAsync(ReceiveInboxMessageCompletedSagaEvent aggregateEvent)
    {
        var item = aggregateEvent.MessageItem;

        var updates = updatesLayeredService.Converter.ToInboxForwardMessageUpdates(aggregateEvent);
        if (aggregateEvent.MessageItem.FwdHeader?.FromId?.PeerType == PeerType.Channel)
        {
            if (updates is TUpdates tUpdates)
            {
                var channelId = aggregateEvent.MessageItem.FwdHeader.FromId.PeerId;
                var channelReadModel = await channelAppService.GetAsync(channelId);
                var photoReadModel = channelReadModel!.PhotoId.HasValue
                    ? await photoAppService.GetAsync(channelReadModel.PhotoId.Value)
                    : null;

                var channel = chatLayeredService.Converter.ToChannel(
                    0,
                    channelReadModel,
                    photoReadModel,
                    null,
                    false);
                tUpdates.Chats.Add(channel);
            }
        }

        await PushUpdatesToPeerAsync(item.OwnerPeer,
            updates,
            pts: item.Pts);
    }

    private async Task HandleInviteToChannelAsync(SendOutboxMessageCompletedSagaEvent aggregateEvent)
    {
        var item = aggregateEvent.MessageItem;

        if (chatEventCacheHelper.TryRemoveStartInviteToChannelEvent(item.ToPeer.PeerId,
                out var startInviteToChannelEvent))
        {
            var channelReadModel = await channelAppService.GetAsync(item.ToPeer.PeerId);

            var updates = updatesLayeredService.GetConverter(aggregateEvent.RequestInfo.Layer)
                .ToInviteToChannelUpdates(
                    aggregateEvent,
                    startInviteToChannelEvent,
                    channelReadModel!,
                    true);
            var invitedUsers = new TInvitedUsers
            {
                Updates = updates,
                MissingInvitees = new TVector<IMissingInvitee>()
            };

            await SendRpcMessageToClientAsync(aggregateEvent.RequestInfo,
                invitedUsers,
                item.SenderPeer.PeerId);

            var updatesForChannelMember = updatesLayeredService.Converter.ToInviteToChannelUpdates(aggregateEvent,
                startInviteToChannelEvent,
                channelReadModel!,
                false
            );

            foreach (var memberUserId in startInviteToChannelEvent.MemberUidList)
            {
                await PushUpdatesToChannelSingleMemberAsync(item.ToPeer.PeerId, memberUserId.ToUserPeer(),
                    updatesForChannelMember);
            }

            await PushUpdatesToChannelMemberAsync(
                item.SenderPeer,
                item.ToPeer,
                updatesForChannelMember,
                excludeUserId: item.SenderPeer.PeerId,
                pts: item.Pts);
        }
    }

    private async Task HandleReceiveMessageAsync(ReceiveInboxMessageCompletedSagaEvent aggregateEvent)
    {
        //var updates = updatesLayeredService.Converter.ToUpdates(aggregateEvent);

        //var layeredData = updatesLayeredService.GetLayeredData(c => c.ToUpdates(aggregateEvent));
        var updates = sendMessageDataConverter.Convert(aggregateEvent);
        var item = aggregateEvent.MessageItems.Last();
        await PushUpdatesToPeerAsync(item.OwnerPeer,
            updates,
            pts: item.Pts,
            //layeredData: layeredData,
            senderUserId: item.SenderPeer.PeerId
        );
    }

    private Task HandleReceiveMessageCompletedAsync(ReceiveInboxMessageCompletedSagaEvent aggregateEvent)
    {
        var item = aggregateEvent.MessageItem;
        return item.MessageSubType switch
        {
            MessageSubType.CreateChat => HandleCreateChatAsync(aggregateEvent),
            MessageSubType.UpdatePinnedMessage => HandleUpdatePinnedMessageAsync(aggregateEvent),
            MessageSubType.ForwardMessage => HandleForwardMessageAsync(aggregateEvent),
            _ => HandleReceiveMessageAsync(aggregateEvent)
        };
    }

    private async Task HandleSendMessageAsync(SendOutboxMessageCompletedSagaEvent aggregateEvent)
    {
        var item = aggregateEvent.MessageItem;
        //if (item.ScheduleDate.HasValue)
        //{
        //    await HandleSendScheduleMessageAsync(aggregateEvent);
        //    return;
        //}
        if (item.ToPeer.PeerType == PeerType.Channel)
        {
            await HandleSendMessageToChannelAsync(aggregateEvent);
            return;
        }

        var selfUpdates = sendMessageDataConverter.Convert(aggregateEvent);
        var updatesType = UpdatesType.Updates;
        if (item.MessageSubType == MessageSubType.Normal ||
            item.MessageSubType == MessageSubType.ForwardMessage)
        {
            updatesType = UpdatesType.NewMessages;
        }

        // when reqMsgId==0?
        // forward message/bot message
        if (aggregateEvent.RequestInfo.ReqMsgId == 0 || item.MessageSubType == MessageSubType.PhoneCall)
        {
            await PushUpdatesToPeerAsync(item.SenderPeer,
                selfUpdates,
                pts: item.Pts,
                updatesType: updatesType
            );
        }
        else
        {
            await ReplyRpcResultToSenderAsync(aggregateEvent.RequestInfo,
                item.SenderPeer,
                selfUpdates,
                item.SenderPeer.PeerId,
                item.Pts
            );
        }

        var selfOtherDeviceUpdates = sendMessageDataConverter.ToSelfOtherDeviceUpdates(aggregateEvent);

        long? excludeAuthKeyId = aggregateEvent.RequestInfo.AuthKeyId;


        await PushUpdatesToPeerAsync(item.SenderPeer,
            selfOtherDeviceUpdates,
            excludeAuthKeyId,
            pts: item.Pts,
            updatesType: updatesType
        );
    }

    private async Task HandleSendMessageToChannelAsync(SendOutboxMessageCompletedSagaEvent aggregateEvent)
    {
        var item = aggregateEvent.MessageItem;
        var selfUpdates = sendMessageDataConverter.Convert(aggregateEvent);
        var selfOtherDeviceUpdates = sendMessageDataConverter.ToSelfOtherDeviceUpdates(aggregateEvent);

        IChannelReadModel? channelReadModel = null;
        IPhotoReadModel? photoReadModel = null;
        IChat? chat = null;
        var isEditChannelPhoto = item.MessageSubType == MessageSubType.EditChannelPhoto;
        if (isEditChannelPhoto)
        {
            (channelReadModel, photoReadModel) = await GetChannelAsync(item.ToPeer.PeerId);
            chat = chatLayeredService.GetConverter(aggregateEvent.RequestInfo.Layer)
                .ToChannel(0, channelReadModel, photoReadModel, null, false);
        }

        var channelUpdates = updatesLayeredService.GetConverter(aggregateEvent.RequestInfo.Layer)
            .ToChannelMessageUpdates(aggregateEvent);

        var updatesType = UpdatesType.Updates;
        if (item.MessageSubType == MessageSubType.Normal || item.MessageSubType == MessageSubType.ForwardMessage)
        {
            updatesType = UpdatesType.NewMessages;
        }

        if (isEditChannelPhoto)
        {
            var selfChannel = chatLayeredService.GetConverter(aggregateEvent.RequestInfo.Layer)
                .ToChannel(aggregateEvent.RequestInfo.UserId, channelReadModel!, photoReadModel, null, false);

            if (selfUpdates is TUpdates tUpdates)
            {
                tUpdates.Chats.Add(selfChannel);
            }

            if (selfOtherDeviceUpdates is TUpdates tSelfOtherDeviceUpdates)
            {
                tSelfOtherDeviceUpdates.Chats.Add(selfChannel);
            }

            if (chat != null)
            {
                if (channelUpdates is TUpdates tUpdates1)
                {
                    tUpdates1.Chats.Add(chat);
                }
            }
        }

        var globalSeqNo = await SavePushUpdatesAsync(item.ToPeer.PeerId,
            channelUpdates,
            item.Pts,
            aggregateEvent.RequestInfo.AuthKeyId,
            aggregateEvent.RequestInfo.UserId,
            messageId: item.MessageId
        );
        await AddRpcGlobalSeqNoForAuthKeyIdAsync(aggregateEvent.RequestInfo.ReqMsgId, item.SenderPeer.PeerId,
                globalSeqNo)
            ;

        if (aggregateEvent.RequestInfo.ReqMsgId == 0 /*|| item.MessageSubType == MessageSubType.ForwardMessage*/)
        {
            await PushUpdatesToPeerAsync(new Peer(PeerType.User, aggregateEvent.RequestInfo.UserId),
                selfUpdates,
                pts: item.Pts,
                updatesType: updatesType
            );
        }
        else
        {
            await ReplyRpcResultToSenderAsync(aggregateEvent.RequestInfo,
                item.SenderUserId.ToUserPeer(),
                selfUpdates,
                aggregateEvent.RequestInfo.UserId,
                item.Pts
            );

            await PushUpdatesToPeerAsync(item.SenderUserId.ToUserPeer(),
                selfOtherDeviceUpdates,
                aggregateEvent.RequestInfo.PermAuthKeyId,
                pts: item.Pts,
                updatesType: updatesType
            //layeredData: updatesLayeredService.GetLayeredData(c => c.ToSelfOtherDeviceUpdates(aggregateEvent))
            );
        }

        // notify mentioned users
        if (aggregateEvent.MentionedUserIds?.Count > 0)
        {
            //var mentionedUpdates = updatesLayeredService.GetConverter(aggregateEvent.RequestInfo.Layer)
            //    .ToChannelMessageUpdates(aggregateEvent, true);
            //var layeredMentionUpdates =
            //    updatesLayeredService.GetLayeredData(c => c.ToChannelMessageUpdates(aggregateEvent, true));
            //foreach (var mentionedUserId in aggregateEvent.MentionedUserIds)
            //{
            //    if (mentionedUserId != aggregateEvent.RequestInfo.UserId)
            //    {
            //        await PushUpdatesToPeerAsync(new Peer(PeerType.User, mentionedUserId), mentionedUpdates,
            //            layeredData: layeredMentionUpdates);
            //    }
            //}
        }

        //Console.WriteLine($"Push message to channel:{item.ToPeer} pts:{aggregateEvent.Pts}");
        await PushUpdatesToPeerAsync(item.ToPeer,
                channelUpdates,
                aggregateEvent.RequestInfo.AuthKeyId,
                updatesType: updatesType,
                skipSaveUpdates: true
            )
            ;
    }

    private Task HandleSendOutboxMessageCompletedAsync(SendOutboxMessageCompletedSagaEvent aggregateEvent)
    {
        var item = aggregateEvent.MessageItem;
        return item.MessageSubType switch
        {
            MessageSubType.CreateChat => HandleCreateChatAsync(aggregateEvent),
            MessageSubType.CreateChannel => HandleCreateChannelAsync(aggregateEvent),
            MessageSubType.AutoCreateChannelFromChat => HandleCreateChannelAsync(aggregateEvent),
            MessageSubType.InviteToChannel => HandleInviteToChannelAsync(aggregateEvent),
            MessageSubType.UpdatePinnedMessage => HandleUpdatePinnedMessageAsync(aggregateEvent),
            _ => HandleSendMessageAsync(aggregateEvent)
        };
    }

    private async Task HandleUpdatePinnedMessageAsync(ReceiveInboxMessageCompletedSagaEvent aggregateEvent)
    {
        var updates = updatesLayeredService.Converter.ToUpdatePinnedMessageUpdates(aggregateEvent);
        var item = aggregateEvent.MessageItem;
        await PushUpdatesToPeerAsync(item.OwnerPeer,
            updates,
            pts: item.Pts);
    }

    private async Task HandleUpdatePinnedMessageAsync(SendOutboxMessageCompletedSagaEvent aggregateEvent)
    {
        var item = aggregateEvent.MessageItem;
        var updates = updatesLayeredService.GetConverter(aggregateEvent.RequestInfo.Layer)
            .ToUpdatePinnedMessageUpdates(aggregateEvent);

        await PushUpdatesToPeerAsync(item.SenderPeer,
            updates,
            pts: item.Pts);

        if (item.ToPeer.PeerType == PeerType.Channel)
        {
            var channelUpdates = updatesLayeredService.Converter.ToUpdatePinnedMessageServiceUpdates(aggregateEvent);
            if (channelUpdates is TUpdates tUpdates)
            {
                var user = await GetUserAsync(aggregateEvent.RequestInfo.UserId, 0);
                tUpdates.Users.Add(user);
            }

            var layeredChannelUpdates =
                updatesLayeredService.GetLayeredData(c => c.ToUpdatePinnedMessageServiceUpdates(aggregateEvent));
            await PushUpdatesToPeerAsync(item.ToPeer,
                channelUpdates,
                aggregateEvent.RequestInfo.AuthKeyId,
                pts: item.Pts,
                layeredData: layeredChannelUpdates
            );
        }
    }

    public Task HandleAsync(
        IDomainEvent<SendMessageSaga, SendMessageSagaId, ReceiveInboxMessageCompletedSagaEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        return HandleReceiveMessageCompletedAsync(domainEvent.AggregateEvent);
    }

    public Task HandleAsync(
        IDomainEvent<SendMessageSaga, SendMessageSagaId, SendOutboxMessageCompletedSagaEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        return HandleSendOutboxMessageCompletedAsync(domainEvent.AggregateEvent);
    }
}