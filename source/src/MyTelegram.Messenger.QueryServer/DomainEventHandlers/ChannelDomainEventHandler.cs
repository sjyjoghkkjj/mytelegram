using MyTelegram.Domain.Aggregates.ChatInvite;
using MyTelegram.Domain.Events.ChatInvite;
using MyTelegram.Messenger.Services.Caching;
using MyTelegram.Messenger.Services.Interfaces;
using MyTelegram.Messenger.TLObjectConverters.Interfaces;
using MyTelegram.Services.TLObjectConverters;

namespace MyTelegram.Messenger.QueryServer.DomainEventHandlers;

public class ChannelDomainEventHandler(
    IObjectMessageSender objectMessageSender,
    ICommandBus commandBus,
    IIdGenerator idGenerator,
    IAckCacheService ackCacheService,
    IResponseCacheAppService responseCacheAppService,
    IChatEventCacheHelper chatEventCacheHelper,
    IChannelAppService channelAppService,
    ILayeredService<IChatConverter> chatLayeredService,
    IPhotoAppService photoAppService,
    ILayeredService<IUpdatesConverter> layeredUpdatesService)
    : DomainEventHandlerBase(objectMessageSender,
            commandBus,
            idGenerator,
            ackCacheService,
            responseCacheAppService),
        //ISubscribeSynchronousTo<ChannelAggregate, ChannelId, ChannelCreatedEvent>,
        //ISubscribeSynchronousTo<ChannelAggregate, ChannelId, ChannelInviteExportedEvent>,
        //ISubscribeSynchronousTo<ChannelAggregate, ChannelId, StartInviteToChannelEvent>,
        ISubscribeSynchronousTo<ChannelAggregate, ChannelId, DiscussionGroupUpdatedEvent>,
        ISubscribeSynchronousTo<ChannelAggregate, ChannelId, ChannelTitleEditedEvent>,
        ISubscribeSynchronousTo<ChannelAggregate, ChannelId, ChannelAboutEditedEvent>,
        ISubscribeSynchronousTo<ChannelAggregate, ChannelId, ChannelDefaultBannedRightsEditedEvent>,
        ISubscribeSynchronousTo<ChannelAggregate, ChannelId, SlowModeChangedEvent>,
        ISubscribeSynchronousTo<ChannelAggregate, ChannelId, PreHistoryHiddenChangedEvent>,
        ISubscribeSynchronousTo<ChannelAggregate, ChannelId, ChannelAdminRightsEditedEvent>,
        ISubscribeSynchronousTo<ChannelAggregate, ChannelId, ChannelUserNameChangedEvent>,
        ISubscribeSynchronousTo<ChannelMemberAggregate, ChannelMemberId, ChannelMemberJoinedEvent>,
        ISubscribeSynchronousTo<ChannelMemberAggregate, ChannelMemberId, ChannelMemberBannedRightsChangedEvent>,
        ISubscribeSynchronousTo<ChannelMemberAggregate, ChannelMemberId, ChannelMemberLeftEvent>,
        ISubscribeSynchronousTo<InviteToChannelSaga, InviteToChannelSagaId, InviteToChannelCompletedSagaEvent>,
        //ISubscribeSynchronousTo<ChannelAggregate, ChannelId, ChannelInviteEditedEvent>
        ISubscribeSynchronousTo<ChannelAggregate, ChannelId, ChannelColorUpdatedEvent>,
        ISubscribeSynchronousTo<ChatInviteAggregate, ChatInviteId, ChatInviteCreatedEvent>,
        ISubscribeSynchronousTo<ChatInviteAggregate, ChatInviteId, ChatInviteEditedEvent>,
        ISubscribeSynchronousTo<ChatInviteAggregate, ChatInviteId, ChatInviteImportedEvent>,
        ISubscribeSynchronousTo<ChannelAggregate, ChannelId, ChatInviteRequestPendingUpdatedEvent>,
        ISubscribeSynchronousTo<ChannelAggregate, ChannelId, ChatJoinRequestHiddenEvent>,
        ISubscribeSynchronousTo<ChannelAggregate, ChannelId, ChannelDeletedEvent>,
        ISubscribeSynchronousTo<UpdatePinnedMessageSaga, UpdatePinnedMessageSagaId, UpdatePinnedMessageCompletedSagaEvent>
{
    public Task HandleAsync(IDomainEvent<ChannelAggregate, ChannelId, ChannelAboutEditedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        return SendRpcMessageToClientAsync(domainEvent.AggregateEvent.RequestInfo,
            new TBoolTrue());
    }

    public async Task HandleAsync(
        IDomainEvent<ChannelAggregate, ChannelId, ChannelAdminRightsEditedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        await NotifyUpdateChannelAsync(domainEvent.AggregateEvent.RequestInfo,
                domainEvent.AggregateEvent.ChannelId);

        var (channelReadModel, photoReadModel) = await GetChannelAsync(domainEvent.AggregateEvent.ChannelId);
        var channel = chatLayeredService.Converter.ToChannel(domainEvent.AggregateEvent.UserId, channelReadModel,
            photoReadModel,
            null, false);
        var updates = new TUpdates
        {
            Updates = new TVector<IUpdate>([
                new TUpdateChannel
                {
                    ChannelId = domainEvent.AggregateEvent.ChannelId
                }
            ]),
            Chats = new TVector<IChat>([channel]),
            Users = new TVector<IUser>(),
            Date = DateTime.UtcNow.ToTimestamp()
        };
        await PushMessageToPeerAsync(domainEvent.AggregateEvent.UserId.ToUserPeer(), updates);
    }

    public async Task HandleAsync(IDomainEvent<ChannelAggregate, ChannelId, ChannelColorUpdatedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        var item = await GetChannelAsync(domainEvent.AggregateEvent.ChannelId);
        await NotifyUpdateChannelAsync(domainEvent.AggregateEvent.RequestInfo, domainEvent.AggregateEvent.ChannelId,
            0,
            item.Item1,
            item.Item2
        );
    }

    public async Task HandleAsync(
        IDomainEvent<ChannelAggregate, ChannelId, ChannelDefaultBannedRightsEditedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        await NotifyUpdateChannelAsync(domainEvent.AggregateEvent.RequestInfo,
            domainEvent.AggregateEvent.ChannelId);
    }

    public async Task HandleAsync(IDomainEvent<ChannelAggregate, ChannelId, ChannelDeletedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        var channelForbidden = new TChannelForbidden
        {
            Id = domainEvent.AggregateEvent.ChannelId,
            Broadcast = domainEvent.AggregateEvent.Broadcast,
            Megagroup = domainEvent.AggregateEvent.Megagroup,
            AccessHash = domainEvent.AggregateEvent.AccessHash,
            Title = domainEvent.AggregateEvent.Title
        };
        var updates = new TUpdates
        {
            Updates = new TVector<IUpdate>(),
            Users = new TVector<IUser>(),
            Chats = new TVector<IChat>(channelForbidden),
            Date = DateTime.UtcNow.ToTimestamp()
        };
        await SendRpcMessageToClientAsync(domainEvent.AggregateEvent.RequestInfo, updates);
        await PushUpdatesToPeerAsync(domainEvent.AggregateEvent.RequestInfo.UserId.ToUserPeer(), updates);
        var updateChannel = new TUpdateChannel
        {
            ChannelId = domainEvent.AggregateEvent.ChannelId
        };
        var updatesChannel = new TUpdates
        {
            Updates = new TVector<IUpdate>(updateChannel),
            Users = new TVector<IUser>(),
            Chats = new TVector<IChat>(channelForbidden)
        };
        await PushUpdatesToPeerAsync(domainEvent.AggregateEvent.ChannelId.ToChannelPeer(), updatesChannel,
            excludeUserId: domainEvent.AggregateEvent.RequestInfo.UserId);
    }
    public async Task HandleAsync(IDomainEvent<ChannelAggregate, ChannelId, ChannelTitleEditedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        await NotifyUpdateChannelAsync(domainEvent.AggregateEvent.RequestInfo with { ReqMsgId = 0 },
            domainEvent.AggregateEvent.ChannelId);
    }

    public Task HandleAsync(IDomainEvent<ChannelAggregate, ChannelId, ChannelUserNameChangedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        return SendRpcMessageToClientAsync(domainEvent.AggregateEvent.RequestInfo, new TBoolTrue());
    }

    public async Task HandleAsync(
        IDomainEvent<ChannelAggregate, ChannelId, ChatInviteRequestPendingUpdatedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        // We should notify all channel admins to approve the request after chatInvites imported 
        var update = new TUpdatePendingJoinRequests
        {
            Peer = new TPeerChannel
            {
                ChannelId = domainEvent.AggregateEvent.ChannelId
            },
            RequestsPending = domainEvent.AggregateEvent.RequestsPending ?? 0,
            RecentRequesters = new TVector<long>(domainEvent.AggregateEvent.RecentRequesters)
        };

        var updates = new TUpdates
        {
            Updates = new TVector<IUpdate>(update),
            Chats = new TVector<IChat>(),
            Date = DateTime.UtcNow.ToTimestamp(),
            Users = new TVector<IUser>()
        };

        foreach (var userId in domainEvent.AggregateEvent.ChannelAdmins)
        {
            await PushMessageToPeerAsync(new Peer(PeerType.User, userId), updates);
        }
    }

    public Task HandleAsync(IDomainEvent<ChannelAggregate, ChannelId, ChatJoinRequestHiddenEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        var update = new TUpdateChannel
        {
            ChannelId = domainEvent.AggregateEvent.ChannelId
        };
        var updates = new TUpdates
        {
            Chats = new TVector<IChat>(),
            Date = DateTime.UtcNow.ToTimestamp(),
            Updates = new TVector<IUpdate>(update),
            Users = new TVector<IUser>()
        };

        return SendRpcMessageToClientAsync(domainEvent.AggregateEvent.RequestInfo, updates);
    }

    public Task HandleAsync(IDomainEvent<ChannelAggregate, ChannelId, DiscussionGroupUpdatedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        return SendRpcMessageToClientAsync(domainEvent.AggregateEvent.RequestInfo,
            new TBoolTrue());
    }

    public async Task HandleAsync(
        IDomainEvent<ChannelAggregate, ChannelId, PreHistoryHiddenChangedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        await NotifyUpdateChannelAsync(domainEvent.AggregateEvent.RequestInfo,
                domainEvent.AggregateEvent.ChannelId);
    }

    public async Task HandleAsync(IDomainEvent<ChannelAggregate, ChannelId, SlowModeChangedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        await NotifyUpdateChannelAsync(domainEvent.AggregateEvent.RequestInfo,
                domainEvent.AggregateEvent.ChannelId);
    }

    public Task HandleAsync(
        IDomainEvent<ChannelMemberAggregate, ChannelMemberId, ChannelMemberBannedRightsChangedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        return NotifyUpdateChannelAsync(domainEvent.AggregateEvent.RequestInfo,
            domainEvent.AggregateEvent.ChannelId,
            domainEvent.AggregateEvent.MemberUserId);
    }

    public Task HandleAsync(
        IDomainEvent<ChannelMemberAggregate, ChannelMemberId, ChannelMemberJoinedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        return SendChannelUpdatedRpcResultAndNotifyChannelMembersAsync(domainEvent.AggregateEvent.ChannelId,
            domainEvent.AggregateEvent.MemberUserId, domainEvent.AggregateEvent.RequestInfo,
            domainEvent.AggregateEvent.Date);
    }

    public async Task HandleAsync(
        IDomainEvent<ChannelMemberAggregate, ChannelMemberId, ChannelMemberLeftEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        var channelReadModel = await channelAppService.GetAsync(domainEvent.AggregateEvent.ChannelId);
        var photoReadModel = await photoAppService.GetAsync(channelReadModel!.PhotoId);
        var channel = chatLayeredService.GetConverter(domainEvent.AggregateEvent.RequestInfo.Layer)
            .ToChannel(domainEvent.AggregateEvent.RequestInfo.UserId, channelReadModel, photoReadModel, null, true);
        var updates = new TUpdates
        {
            Updates = [new TUpdateChannel
            {
                ChannelId = channelReadModel.ChannelId
            }],
            Users = [],
            Chats = [channel]
        };
        await SendRpcMessageToClientAsync(domainEvent.AggregateEvent.RequestInfo, updates);
        await PushUpdatesToPeerAsync(domainEvent.AggregateEvent.RequestInfo.UserId.ToUserPeer(), updates,
            excludeAuthKeyId: domainEvent.AggregateEvent.RequestInfo.PermAuthKeyId);
    }

    public Task HandleAsync(IDomainEvent<ChatInviteAggregate, ChatInviteId, ChatInviteCreatedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        var data = chatLayeredService.GetConverter(domainEvent.AggregateEvent.RequestInfo.Layer)
            .ToExportedChatInvite(domainEvent.AggregateEvent);
        return SendRpcMessageToClientAsync(domainEvent.AggregateEvent.RequestInfo, data);
    }

    public Task HandleAsync(IDomainEvent<ChatInviteAggregate, ChatInviteId, ChatInviteEditedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        var exportedChatInvite = chatLayeredService.GetConverter(domainEvent.AggregateEvent.RequestInfo.Layer)
            .ToExportedChatInvite(domainEvent.AggregateEvent);

        return SendRpcMessageToClientAsync(domainEvent.AggregateEvent.RequestInfo, exportedChatInvite);
    }

    public async Task HandleAsync(IDomainEvent<ChatInviteAggregate, ChatInviteId, ChatInviteImportedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        if (domainEvent.AggregateEvent.ChatInviteRequestState == ChatInviteRequestState.NeedApprove)
        {
            var rpcError = new TRpcError
            {
                ErrorCode = RpcErrors.RpcErrors400.InviteRequestSent.ErrorCode,
                ErrorMessage = RpcErrors.RpcErrors400.InviteRequestSent.Message
            };

            await SendRpcMessageToClientAsync(domainEvent.AggregateEvent.RequestInfo, rpcError);

            //// We should notify all channel admins to approve the request after chatInvites imported 
            //var update = new TUpdatePendingJoinRequests
            //{
            //    Peer = new TPeerChannel
            //    {
            //        ChannelId = domainEvent.AggregateEvent.ChannelId,
            //    },
            //    RequestsPending = domainEvent.AggregateEvent.RequestsPending ?? 0,
            //    RecentRequesters = new TVector<long>(domainEvent.AggregateEvent.RecentRequesters)
            //};

            //var updates = new TUpdates
            //{
            //    Updates = new TVector<IUpdate>(update),
            //    Chats = new(),
            //    Date = DateTime.UtcNow.ToTimestamp(),
            //    Users = new()
            //};

            //foreach (var userId in domainEvent.AggregateEvent.ChatAdmins)
            //{
            //    await SendMessageToPeerAsync(new Peer(PeerType.User, userId), updates);
            //}
        }
    }

    public async Task HandleAsync(
        IDomainEvent<InviteToChannelSaga, InviteToChannelSagaId, InviteToChannelCompletedSagaEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        if (domainEvent.AggregateEvent.Broadcast || domainEvent.AggregateEvent.HasLink)
        {
            var updates = new TUpdateShort
            {
                Date = DateTime.UtcNow.ToTimestamp(),
                Update = new TUpdateChannel
                {
                    ChannelId = domainEvent.AggregateEvent.ChannelId
                }
            };
            var invitedUsers = new TInvitedUsers
            {
                Updates = updates,
                MissingInvitees = new TVector<IMissingInvitee>()
            };
            await SendRpcMessageToClientAsync(domainEvent.AggregateEvent.RequestInfo, invitedUsers);

            var item = await GetChannelAsync(domainEvent.AggregateEvent.ChannelId);
            var inviteToChannelDefaultPts = 1;
            foreach (var userId in domainEvent.AggregateEvent.MemberUidList)
            {
                await NotifyUpdateChannelAsync(
                    domainEvent.AggregateEvent.RequestInfo,
                    domainEvent.AggregateEvent.ChannelId,
                    userId,
                    item.Item1,
                    item.Item2,
                    inviteToChannelDefaultPts
                );
            }
        }
    }

    public async Task HandleAsync(
        IDomainEvent<UpdatePinnedMessageSaga, UpdatePinnedMessageSagaId, UpdatePinnedMessageCompletedSagaEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        var r = layeredUpdatesService.GetConverter(domainEvent.AggregateEvent.RequestInfo.Layer)
            .ToSelfUpdatePinnedMessageUpdates(domainEvent.AggregateEvent);
        if (domainEvent.AggregateEvent.PmOneSide || domainEvent.AggregateEvent.ShouldReplyRpcResult)
        {
            await SendRpcMessageToClientAsync(domainEvent.AggregateEvent.RequestInfo,
                r,
                domainEvent.AggregateEvent.SenderPeerId,
                domainEvent.AggregateEvent.Pts,
                //PtsType.OtherUpdates,
                domainEvent.AggregateEvent.ToPeer.PeerType
            );
            var layeredData =
                layeredUpdatesService.GetLayeredData(c =>
                    c.ToSelfUpdatePinnedMessageUpdates(domainEvent.AggregateEvent));
            await PushUpdatesToPeerAsync(
                new Peer(PeerType.User, domainEvent.AggregateEvent.OwnerPeerId),
                r,
                pts: domainEvent.AggregateEvent.Pts,
                layeredData: layeredData);
        }

        //else
        {
            var updates = layeredUpdatesService.Converter.ToUpdatePinnedMessageUpdates(domainEvent.AggregateEvent);
            if (domainEvent.AggregateEvent.ToPeer.PeerType == PeerType.Channel)
            {
                //var (channelReadModel, photoReadModel) =
                //    await GetChannelAsync(domainEvent.AggregateEvent.ToPeer.PeerId);
            }

            await PushUpdatesToPeerAsync(
                domainEvent.AggregateEvent.ToPeer.PeerType == PeerType.Channel
                    ? new Peer(PeerType.Channel, domainEvent.AggregateEvent.OwnerPeerId)
                    : new Peer(PeerType.User, domainEvent.AggregateEvent.OwnerPeerId),
                updates,
                excludeUserId: domainEvent.AggregateEvent.SenderPeerId,
                pts: domainEvent.AggregateEvent.Pts,
                layeredData: layeredUpdatesService.GetLayeredData(c =>
                    c.ToUpdatePinnedMessageUpdates(domainEvent.AggregateEvent))
            );

            if (domainEvent.AggregateEvent.ToPeer.PeerType == PeerType.Channel)
            {
                await NotifyUpdateChannelAsync(domainEvent.AggregateEvent.RequestInfo,
                    domainEvent.AggregateEvent.ToPeer.PeerId);
            }
        }
    }
    public Task HandleAsync(IDomainEvent<ChannelAggregate, ChannelId, ChannelCreatedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        chatEventCacheHelper.Add(domainEvent.AggregateEvent);
        return Task.CompletedTask;
    }

    public Task HandleAsync(IDomainEvent<ChannelAggregate, ChannelId, StartInviteToChannelEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        chatEventCacheHelper.Add(domainEvent.AggregateEvent);
        return Task.CompletedTask;
    }

    private async Task<(IChannelReadModel, IPhotoReadModel?)> GetChannelAsync(long channelId)
    {
        var channelReadModel = await channelAppService.GetAsync(channelId);
        var photoReadModel = await photoAppService.GetAsync(channelReadModel!.PhotoId);

        return (channelReadModel, photoReadModel);
    }


    private async Task NotifyUpdateChannelAsync(RequestInfo requestInfo,
        long channelId,
        long memberUserId = 0,
        IChannelReadModel? channelReadModel = null,
        IPhotoReadModel? channelPhotoReadModel = null,
        int pts = 0
    )
    {
        if (channelReadModel == null)
        {
            var item = await GetChannelAsync(channelId);
            channelReadModel = item.Item1;
            channelPhotoReadModel = item.Item2;
        }

        var updates = layeredUpdatesService.GetConverter(requestInfo.Layer)
            .ToChannelUpdates(memberUserId, channelReadModel, channelPhotoReadModel);

        var layeredUpdates = layeredUpdatesService.GetLayeredData(c =>
            c.ToChannelUpdates(memberUserId, channelReadModel, channelPhotoReadModel));

        await SendRpcMessageToClientAsync(requestInfo, updates);
        if (memberUserId != 0)
        {
            await PushUpdatesToPeerAsync(new Peer(PeerType.Channel, channelId),
                //channelId,
                updates,
                onlySendToUserId: memberUserId,
                layeredData: layeredUpdates, pts: pts);
        }
        else
        {
            await PushUpdatesToPeerAsync(new Peer(PeerType.Channel, channelId),
                //channelId, 
                updates,
                excludeUserId: requestInfo.UserId
            );
        }
    }

    private async Task SendChannelUpdatedRpcResultAndNotifyChannelMembersAsync(long channelId,
        long channelMemberUserId,
        RequestInfo requestInfo,
        int date
    )
    {
        var channelReadModel = await channelAppService.GetAsync(channelId);
        var photoReadModel = await photoAppService.GetAsync(channelReadModel!.PhotoId);

        var updates = new TUpdates
        {
            Chats = new TVector<IChat>(chatLayeredService.GetConverter(requestInfo.Layer)
                .ToChannel(
                    channelMemberUserId,
                    channelReadModel,
                    photoReadModel,
                    null,
                    false)),
            Date = date,
            Seq = 0,
            Users = new TVector<IUser>(),
            Updates = new TVector<IUpdate>()
        };

        await SendRpcMessageToClientAsync(requestInfo, updates);
        await NotifyUpdateChannelAsync(requestInfo with { ReqMsgId = 0 }, channelId);
    }
}