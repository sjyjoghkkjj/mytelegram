using MyTelegram.Messenger.Services.Caching;
using MyTelegram.Messenger.Services.Interfaces;

namespace MyTelegram.Messenger.QueryServer.DomainEventHandlers;

public partial class MessageDomainEventHandler(
    IObjectMessageSender objectMessageSender,
    ICommandBus commandBus,
    IIdGenerator idGenerator,
    IAckCacheService ackCacheService,
    IChatEventCacheHelper chatEventCacheHelper,
    ILogger<MessageDomainEventHandler> logger,
    IQueryProcessor queryProcessor,
    IUpdatesConverterService updatesConverterService,
    IMessageConverterService messageConverterService,
    IUserConverterService userConverterService,
    IChatConverterService chatConverterService,
    IChannelAppService channelAppService,
    ISendMessageConverterService sendMessageConverterService,
    IEditMessageConverterService editMessageConverterService,
    IInviteToChannelConverterService inviteToChannelConverterService,
    IPhotoAppService photoAppService)
    : DomainEventHandlerBase(objectMessageSender,
            commandBus,
            idGenerator,
            ackCacheService),
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
        var updates = editMessageConverterService.ToEditMessageUpdates(domainEvent.AggregateEvent);
        return PushUpdatesToPeerAsync(toPeer,
            updates,
            pts: domainEvent.AggregateEvent.NewMessageItem.Pts,
            updatesType: UpdatesType.Updates
        );
    }

    public async Task HandleAsync(
        IDomainEvent<EditMessageSaga, EditMessageSagaId, OutboxMessageEditCompletedSagaEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        if (domainEvent.AggregateEvent.NewMessageItem.QuickReplyItem != null)
        {
            await HandleEditQuickReplyMessageAsync(domainEvent.AggregateEvent);
            return;
        }

        var updates = editMessageConverterService.ToEditMessageUpdates(domainEvent.AggregateEvent.RequestInfo.UserId, domainEvent.AggregateEvent, domainEvent.AggregateEvent.RequestInfo.Layer);
        var updatesForOtherParticipant = editMessageConverterService.ToEditMessageUpdates(-1, domainEvent.AggregateEvent, 0);

        await SendRpcMessageToClientAsync(domainEvent.AggregateEvent.RequestInfo,
            updates,
            domainEvent.AggregateEvent.NewMessageItem.SenderPeer.PeerId,
            domainEvent.AggregateEvent.NewMessageItem.Pts,
            domainEvent.AggregateEvent.NewMessageItem.ToPeer.PeerType
        );
        await PushUpdatesToPeerAsync(
            domainEvent.AggregateEvent.NewMessageItem.SenderUserId.ToUserPeer(),
            updatesForOtherParticipant,
            domainEvent.AggregateEvent.RequestInfo.AuthKeyId,
            pts: domainEvent.AggregateEvent.NewMessageItem.Pts,
            updatesType: UpdatesType.Updates);

        // Channel message shares the same message,edit out message should notify channel member
        if (domainEvent.AggregateEvent.NewMessageItem.ToPeer.PeerType == PeerType.Channel)
        {
            var channelEditUpdates = editMessageConverterService.ToEditMessageUpdates(-1, domainEvent.AggregateEvent, 0);

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
            //var message = messageLayeredService.Converter.ToMessage(messageReadModel, null, null, 0);

            var message = messageConverterService.ToMessage(messageReadModel.SenderUserId, messageReadModel);
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

    private Task<ILayeredUser> GetUserAsync(long userId, long selfUserId)
    {
        return userConverterService.GetUserAsync(selfUserId, userId);
        //var userReadModel = await userAppService.GetAsync(userId);
        //var photos = await photoAppService.GetPhotosAsync(userReadModel);
        //var privacyList = await privacyAppService.GetPrivacyListAsync(userId);
        //return userLayeredService.Converter.ToUser(selfUserId, userReadModel!, photos, privacies: privacyList);
    }

    private async Task HandleCreateChannelAsync(SendOutboxMessageCompletedSagaEvent aggregateEvent)
    {
        var item = aggregateEvent.MessageItem;
        if (chatEventCacheHelper.TryRemoveChannelCreatedEvent(item.ToPeer.PeerId,
                out var eventData))
        {
            var channelId = eventData.ChannelId;
            //var updates = updatesLayeredService.GetConverter(aggregateEvent.RequestInfo.Layer)
            //    .ToCreateChannelUpdates(eventData, aggregateEvent);
            var updates = await updatesConverterService.ToCreateChannelUpdatesAsync(eventData, aggregateEvent, true, aggregateEvent.RequestInfo.Layer);
            var updatesForSelfOtherDevices = await updatesConverterService.ToCreateChannelUpdatesAsync(eventData, aggregateEvent, true, 0);
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
                updatesForSelfOtherDevices,
                aggregateEvent.RequestInfo.AuthKeyId,
                pts: item.Pts
            );
        }
        else
        {
            logger.LogWarning("Cannot find cached channel info, channelId: {ChannelId}",
                item.ToPeer.PeerId);
        }
    }

    private async Task HandleEditQuickReplyMessageAsync(OutboxMessageEditCompletedSagaEvent aggregateEvent)
    {
        var updates = editMessageConverterService.ToEditQuickReplyMessageUpdates(aggregateEvent, aggregateEvent.RequestInfo.Layer);
        var updatesForSelfOtherDevices = editMessageConverterService.ToEditQuickReplyMessageUpdates(aggregateEvent, 0);
        await SendRpcMessageToClientAsync(aggregateEvent.RequestInfo, updates);
        await PushUpdatesToPeerAsync(aggregateEvent.RequestInfo.UserId.ToUserPeer(), updatesForSelfOtherDevices,
            aggregateEvent.RequestInfo.PermAuthKeyId);
    }
    private async Task HandleForwardMessageAsync(ReceiveInboxMessageCompletedSagaEvent aggregateEvent)
    {
        var item = aggregateEvent.MessageItem;

        var updates = updatesConverterService.ToInboxForwardMessageUpdates(aggregateEvent);
        if (aggregateEvent.MessageItem.FwdHeader?.FromId?.PeerType == PeerType.Channel)
        {
            if (updates is TUpdates tUpdates)
            {
                var channelId = aggregateEvent.MessageItem.FwdHeader.FromId.PeerId;
                //var channelReadModel = await channelAppService.GetAsync(channelId);
                //var photoReadModel = channelReadModel!.PhotoId.HasValue
                //    ? await photoAppService.GetAsync(channelReadModel.PhotoId.Value)
                //    : null;
                var channel = await chatConverterService.GetChannelAsync(0, channelId, false, false);



                //var channel = chatLayeredService.Converter.ToChannel(
                //    0,
                //    channelReadModel,
                //    photoReadModel,
                //    null,
                //    false);
                tUpdates.Chats.Add(channel);
            }
        }

        //var layeredData = updatesLayeredService.GetLayeredData(c => c.ToInboxForwardMessageUpdates(aggregateEvent));
        await PushUpdatesToPeerAsync(item.OwnerPeer,
            updates,
            pts: item.Pts);
    }

    private async Task HandleInviteToChannelAsync(SendOutboxMessageCompletedSagaEvent aggregateEvent)
    {
        var item = aggregateEvent.MessageItem;
        var invitedUsers = inviteToChannelConverterService.ToInvitedUsers(aggregateEvent);
        await UpdateChannelAndUserAsync(aggregateEvent.RequestInfo.UserId, invitedUsers.Updates, item.ToPeer.PeerId, layer: aggregateEvent.RequestInfo.Layer);
        await SendRpcMessageToClientAsync(aggregateEvent.RequestInfo,
            invitedUsers,
            item.SenderPeer.PeerId);

        var updatesForChannelMember = inviteToChannelConverterService.ToInviteToChannelUpdates(aggregateEvent, 0);
        await UpdateChannelAndUserAsync(-1, updatesForChannelMember, item.ToPeer.PeerId, item.SenderUserId, 0);

        if (item is { Post: true, MessageAction: TMessageActionChatAddUser messageActionChatAddUser })
        {
            foreach (var userId in messageActionChatAddUser.Users)
            {
                await PushUpdatesToChannelSingleMemberAsync(item.ToPeer.PeerId, userId.ToUserPeer(),
                    updatesForChannelMember);
            }

            return;
        }

        await PushMessageToPeerAsync(item.ToPeer, updatesForChannelMember, excludeAuthKeyId: aggregateEvent.RequestInfo.PermAuthKeyId);
    }

    private async Task HandleReceiveMessageAsync(ReceiveInboxMessageCompletedSagaEvent aggregateEvent)
    {
        var updates = sendMessageConverterService.ToUpdates(aggregateEvent);
        var item = aggregateEvent.MessageItems.Last();
        await PushUpdatesToPeerAsync(item.OwnerPeer,
            updates,
            pts: item.Pts,
            senderUserId: item.SenderPeer.PeerId
        );
    }

    private Task HandleReceiveMessageCompletedAsync(ReceiveInboxMessageCompletedSagaEvent aggregateEvent)
    {
        var item = aggregateEvent.MessageItem;
        return item.MessageSubType switch
        {
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

        var layer = aggregateEvent.RequestInfo.Layer;
        if (aggregateEvent.RequestInfo.ReqMsgId == 0 || item.MessageSubType == MessageSubType.PhoneCall)
        {
            layer = 0;
        }

        var selfUpdates = sendMessageConverterService.ToUpdates(aggregateEvent, layer);
        var selfOtherDeviceUpdates = sendMessageConverterService.ToSelfOtherDeviceUpdates(aggregateEvent, 0);

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


        long? excludeAuthKeyId = aggregateEvent.RequestInfo.AuthKeyId;
        if (item.MessageActionType == MessageActionType.SetChatTheme)
        {
            excludeAuthKeyId = null;
        }

        await PushUpdatesToPeerAsync(item.SenderPeer,
            selfOtherDeviceUpdates,
            excludeAuthKeyId,
            pts: item.Pts,
            updatesType: updatesType
        );
    }

    private void SetChannelInfo(long selfUserId, IUpdates updates, IChannelReadModel channelReadModel, IPhotoReadModel? photoReadModel, int layer)
    {
        if (updates is TUpdates tUpdates)
        {
            if (tUpdates.Chats.All(p => p.Id != channelReadModel.ChannelId))
            {
                var channel =
                    chatConverterService.ToChannel(selfUserId, channelReadModel, photoReadModel, null, false, layer);
                tUpdates.Chats.Add(channel);
            }
        }
    }

    private async Task HandleSendMessageToChannelAsync(SendOutboxMessageCompletedSagaEvent aggregateEvent)
    {
        var item = aggregateEvent.MessageItem;
        var selfUpdates = sendMessageConverterService.ToUpdates(aggregateEvent, aggregateEvent.RequestInfo.Layer);
        var selfOtherDeviceUpdates = sendMessageConverterService.ToSelfOtherDeviceUpdates(aggregateEvent, 0);
        //var channelUpdates = updatesConverterService.ToChannelMessageUpdates(aggregateEvent.RequestInfo.UserId, aggregateEvent, aggregateEvent.RequestInfo.Layer);
        var channelAdminUpdates = updatesConverterService.ToChannelMessageUpdates(-1, aggregateEvent, 0);
        var channelMemberUpdates = updatesConverterService.ToChannelMessageUpdates(-1, aggregateEvent, 0);
        var channelReadModel = await channelAppService.GetAsync(item.ToPeer.PeerId);
        var photoReadModel = await photoAppService.GetAsync(channelReadModel!.PhotoId);
        var layer = aggregateEvent.RequestInfo.Layer;

        SetChannelInfo(aggregateEvent.RequestInfo.UserId, selfUpdates, channelReadModel, photoReadModel, layer);
        SetChannelInfo(aggregateEvent.RequestInfo.UserId, selfOtherDeviceUpdates, channelReadModel, photoReadModel, 0);
        SetChannelInfo(-1, channelMemberUpdates, channelReadModel, photoReadModel, 0);

        var updatesType = UpdatesType.Updates;
        if (item.MessageSubType == MessageSubType.Normal || item.MessageSubType == MessageSubType.ForwardMessage)
        {
            updatesType = UpdatesType.NewMessages;
        }

        var globalSeqNo = await SavePushUpdatesAsync(item.ToPeer.PeerId,
            channelMemberUpdates,
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
                selfOtherDeviceUpdates,
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

        var adminUserIds = channelReadModel.AdminList.Select(p => p.UserId).ToList();
        adminUserIds.Insert(0, channelReadModel.CreatorId);

        foreach (var adminUserId in adminUserIds)
        {
            if (adminUserId == aggregateEvent.RequestInfo.UserId)
            {
                continue;
            }

            SetChannelInfo(adminUserId, channelAdminUpdates, channelReadModel, photoReadModel, 0);
            await PushUpdatesToPeerAsync(item.ToPeer,
                channelAdminUpdates,
                aggregateEvent.RequestInfo.AuthKeyId,
                updatesType: updatesType,
                skipSaveUpdates: true
            );
        }

        await PushUpdatesToPeerAsync(item.ToPeer,
                channelMemberUpdates,
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
            //MessageSubType.CreateChat => HandleCreateChatAsync(aggregateEvent),
            MessageSubType.CreateChannel => HandleCreateChannelAsync(aggregateEvent),
            MessageSubType.AutoCreateChannelFromChat => HandleCreateChannelAsync(aggregateEvent),
            MessageSubType.InviteToChannel => HandleInviteToChannelAsync(aggregateEvent),
            MessageSubType.UpdatePinnedMessage => HandleUpdatePinnedMessageAsync(aggregateEvent),
            _ => HandleSendMessageAsync(aggregateEvent)
        };
    }

    private async Task HandleUpdatePinnedMessageAsync(ReceiveInboxMessageCompletedSagaEvent aggregateEvent)
    {
        var updates = updatesConverterService.ToUpdatePinnedMessageUpdates(aggregateEvent);
        var item = aggregateEvent.MessageItem;
        await PushUpdatesToPeerAsync(item.OwnerPeer,
            updates,
            pts: item.Pts);
    }

    private async Task HandleUpdatePinnedMessageAsync(SendOutboxMessageCompletedSagaEvent aggregateEvent)
    {
        var item = aggregateEvent.MessageItem;
        var updates = updatesConverterService.ToUpdatePinnedMessageUpdates(aggregateEvent, 0);

        await PushUpdatesToPeerAsync(item.SenderPeer,
            updates,
            pts: item.Pts);

        if (item.ToPeer.PeerType == PeerType.Channel)
        {
            var channelUpdates = updatesConverterService.ToUpdatePinnedMessageServiceUpdates(0, aggregateEvent, 0);
            if (channelUpdates is TUpdates tUpdates)
            {
                var user = await GetUserAsync(aggregateEvent.RequestInfo.UserId, 0);
                tUpdates.Users.Add(user);
            }

            await PushUpdatesToPeerAsync(item.ToPeer,
                channelUpdates,
                aggregateEvent.RequestInfo.AuthKeyId,
                pts: item.Pts
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

    private async Task UpdateChannelAndUserAsync(long selfUserId, IUpdates updates, long channelId, long? userId = 0, int layer = 0)
    {
        if (updates is TUpdates tUpdates)
        {
            if (tUpdates.Chats.Count == 0)
            {
                //var channelReadModel = await channelAppService.GetAsync(channelId);
                //var channelPhotoReadModel = await photoAppService.GetAsync(channelReadModel?.PhotoId);
                //if (channelReadModel != null)
                //{
                //    var channel =
                //        chatLayeredService.Converter.ToChannel(selfUserId, channelReadModel, channelPhotoReadModel, null,
                //            false);
                //    tUpdates.Chats = [channel];
                //}
                var channel = await chatConverterService.GetChannelAsync(selfUserId, channelId, false, false, layer);
                tUpdates.Chats = [channel];
            }

            if (tUpdates.Users.Count == 0 && userId > 0)
            {

                //var userReadModel = await userAppService.GetAsync(userId);
                //var userPhotoReadModel = await photoAppService.GetAsync(userReadModel?.ProfilePhotoId);
                //if (userReadModel != null)
                //{
                //    IReadOnlyCollection<IPhotoReadModel>? photos = null;
                //    if (userPhotoReadModel != null)
                //    {
                //        photos = [userPhotoReadModel];
                //    }

                //    var user = userLayeredService.Converter.ToUser(selfUserId, userReadModel, photos);
                var user = await userConverterService.GetUserAsync(selfUserId, userId.Value, layer: layer);
                tUpdates.Users = [user];
            }
        }
    }
}