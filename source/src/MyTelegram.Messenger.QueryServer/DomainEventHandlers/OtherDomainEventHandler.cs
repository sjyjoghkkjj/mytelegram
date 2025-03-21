using MyTelegram.Domain.Aggregates.PeerNotifySetting;
using MyTelegram.Domain.Events.PeerNotifySettings;
using MyTelegram.Messenger.Services.Interfaces;

namespace MyTelegram.Messenger.QueryServer.DomainEventHandlers;

public class OtherDomainEventHandler(
    IObjectMessageSender objectMessageSender,
    ICommandBus commandBus,
    IIdGenerator idGenerator,
    IAckCacheService ackCacheService,
    IEventBus eventBus,
    IUpdatesConverterService updatesConverterService,
    IUserConverterService userConverterService,
    ILayeredService<IAuthorizationConverter> layeredAuthorizationService,
    //ILayeredService<IUserConverter> layeredUserService,
    ICacheManager<GlobalPrivacySettingsCacheItem> cacheManager)
    : DomainEventHandlerBase(objectMessageSender,
            commandBus,
            idGenerator,
            ackCacheService),
        ISubscribeSynchronousTo<SignInSaga, SignInSagaId, SignInSuccessSagaEvent>,
        ISubscribeSynchronousTo<SignInSaga, SignInSagaId, SignUpRequiredSagaEvent>,
        ISubscribeSynchronousTo<ClearHistorySaga, ClearHistorySagaId, ClearSingleUserHistoryCompletedSagaEvent>,
        ISubscribeSynchronousTo<PeerNotifySettingsAggregate, PeerNotifySettingsId, PeerNotifySettingsUpdatedEvent>,
        ISubscribeSynchronousTo<UserAggregate, UserId, UserGlobalPrivacySettingsChangedEvent>,
        ISubscribeSynchronousTo<PinForwardedChannelMessageSaga, PinForwardedChannelMessageSagaId,
            PinChannelMessagePtsIncrementedSagaEvent>,
        ISubscribeSynchronousTo<UpdatePinnedMessageSaga, UpdatePinnedMessageSagaId, UpdateSavedMessagesPinnedCompletedSagaEvent>
{
    private readonly IObjectMessageSender _objectMessageSender = objectMessageSender;

    public async Task HandleAsync(
        IDomainEvent<ClearHistorySaga, ClearHistorySagaId, ClearSingleUserHistoryCompletedSagaEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        if (domainEvent.AggregateEvent.IsSelf)
        {
            var r = new TAffectedHistory
            {
                Pts = domainEvent.AggregateEvent.DeletedBoxItem.Pts,
                PtsCount = domainEvent.AggregateEvent.DeletedBoxItem.PtsCount,
                Offset = domainEvent.AggregateEvent.NextMaxId
            };
            await SendRpcMessageToClientAsync(domainEvent.AggregateEvent.RequestInfo,
                r
            );
        }

        var date = DateTime.UtcNow.ToTimestamp();
        var updates = updatesConverterService
            .ToDeleteMessagesUpdates(domainEvent.AggregateEvent.ToPeerType,
                domainEvent.AggregateEvent.DeletedBoxItem,
                date);
        //var layeredData = layeredUpdatesService.GetLayeredData(c =>
        //    c.ToDeleteMessagesUpdates(domainEvent.AggregateEvent.ToPeerType,
        //        domainEvent.AggregateEvent.DeletedBoxItem,
        //        date));
        await PushUpdatesToPeerAsync(
            new Peer(PeerType.User, domainEvent.AggregateEvent.DeletedBoxItem.OwnerPeerId),
            updates,
            pts: domainEvent.AggregateEvent.DeletedBoxItem.Pts);
    }

    public async Task HandleAsync(IDomainEvent<DeleteMessagesSaga4, DeleteMessagesSaga4Id, DeleteSelfMessagesCompletedSagaEvent> domainEvent, CancellationToken cancellationToken)
    {
        var r = new TAffectedMessages
        {
            Pts = domainEvent.AggregateEvent.Pts,
            PtsCount = domainEvent.AggregateEvent.PtsCount
        };
        await SendRpcMessageToClientAsync(domainEvent.AggregateEvent.RequestInfo,
            r,
            domainEvent.AggregateEvent.RequestInfo.UserId, domainEvent.AggregateEvent.Pts);

        var selfOtherDeviceUpdates = updatesConverterService.ToDeleteMessagesUpdates(PeerType.User,
            new DeletedBoxItem(domainEvent.AggregateEvent.RequestInfo.UserId, domainEvent.AggregateEvent.Pts,
                domainEvent.AggregateEvent.PtsCount, domainEvent.AggregateEvent.MessageIds),
            DateTime.UtcNow.ToTimestamp());
        await PushUpdatesToPeerAsync(domainEvent.AggregateEvent.RequestInfo.UserId.ToUserPeer(), selfOtherDeviceUpdates,
            domainEvent.AggregateEvent.RequestInfo.AuthKeyId);
    }

    public Task HandleAsync(IDomainEvent<DeleteMessagesSaga4, DeleteMessagesSaga4Id, DeleteOtherParticipantMessagesCompletedSagaEvent> domainEvent, CancellationToken cancellationToken)
    {
        var updates = updatesConverterService.ToDeleteMessagesUpdates(PeerType.User,
            new DeletedBoxItem(domainEvent.AggregateEvent.UserId, domainEvent.AggregateEvent.Pts,
                domainEvent.AggregateEvent.PtsCount, domainEvent.AggregateEvent.MessageIds),
            DateTime.UtcNow.ToTimestamp());
        //var layeredUpdates = layeredUpdatesService.GetLayeredData(c => c.ToDeleteMessagesUpdates(PeerType.User,
        //    new DeletedBoxItem(domainEvent.AggregateEvent.UserId, domainEvent.AggregateEvent.Pts,
        //        domainEvent.AggregateEvent.PtsCount, domainEvent.AggregateEvent.MessageIds),
        //    DateTime.UtcNow.ToTimestamp()));

        return PushUpdatesToPeerAsync(domainEvent.AggregateEvent.UserId.ToUserPeer(), updates,
            pts: domainEvent.AggregateEvent.Pts);

    }

    public async Task HandleAsync(IDomainEvent<SignInSaga, SignInSagaId, SignInSuccessSagaEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        var tempAuthKeyId = domainEvent.AggregateEvent.TempAuthKeyId;
        var userId = domainEvent.AggregateEvent.UserId;
        await eventBus.PublishAsync(new UserSignInSuccessEvent(
                domainEvent.AggregateEvent.RequestInfo.ReqMsgId,
                tempAuthKeyId,
                domainEvent.AggregateEvent.PermAuthKeyId,
                domainEvent.AggregateEvent.UserId,
                domainEvent.AggregateEvent.HasPassword ? PasswordState.WaitingForVerify : PasswordState.None));

        if (domainEvent.AggregateEvent.HasPassword)
        {
            await SendRpcMessageToClientAsync(domainEvent.AggregateEvent.RequestInfo, RpcErrors.RpcErrors401.SessionPasswordNeeded.ToRpcError());
            return;
        }

        //var userReadModel = await userAppService.GetAsync(userId);
        //var user = layeredUserService.GetConverter(domainEvent.AggregateEvent.RequestInfo.Layer)
        //    .ToUser(userId, userReadModel);
        var user = await userConverterService.GetUserAsync(userId, userId, layer: domainEvent.AggregateEvent.RequestInfo.Layer);
        var r = layeredAuthorizationService.GetConverter(domainEvent.AggregateEvent.RequestInfo.Layer)
            .CreateAuthorization(user);

        await _objectMessageSender.SendRpcMessageToClientAsync(domainEvent.AggregateEvent.RequestInfo,
            r,
            domainEvent.AggregateEvent.RequestInfo.AuthKeyId,
            domainEvent.AggregateEvent.RequestInfo.PermAuthKeyId,
            domainEvent.AggregateEvent.UserId);
    }

    public Task HandleAsync(IDomainEvent<SignInSaga, SignInSagaId, SignUpRequiredSagaEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        return SendRpcMessageToClientAsync(domainEvent.AggregateEvent.RequestInfo,
            layeredAuthorizationService.GetConverter(domainEvent.AggregateEvent.RequestInfo.Layer)
                .CreateSignUpAuthorization());
    }

    public async Task HandleAsync(
        IDomainEvent<UpdatePinnedMessageSaga, UpdatePinnedMessageSagaId, UpdatePinnedMessageCompletedSagaEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        var r = updatesConverterService
            .ToSelfUpdatePinnedMessageUpdates(domainEvent.AggregateEvent);
        if (domainEvent.AggregateEvent.PmOneSide || domainEvent.AggregateEvent.ShouldReplyRpcResult)
        {
            await SendRpcMessageToClientAsync(domainEvent.AggregateEvent.RequestInfo,
                r,
                domainEvent.AggregateEvent.SenderPeerId,
                domainEvent.AggregateEvent.Pts,
                domainEvent.AggregateEvent.ToPeer.PeerType
            );
            //var layeredData =
            //    layeredUpdatesService.GetLayeredData(c =>
            //        c.ToSelfUpdatePinnedMessageUpdates(domainEvent.AggregateEvent));
            await PushUpdatesToPeerAsync(
                new Peer(PeerType.User, domainEvent.AggregateEvent.OwnerPeerId),
                r,
                pts: domainEvent.AggregateEvent.Pts);
        }

        await PushUpdatesToPeerAsync(
            domainEvent.AggregateEvent.ToPeer.PeerType == PeerType.Channel
                ? new Peer(PeerType.Channel, domainEvent.AggregateEvent.OwnerPeerId)
                : new Peer(PeerType.User, domainEvent.AggregateEvent.OwnerPeerId),
            updatesConverterService.ToUpdatePinnedMessageUpdates(domainEvent.AggregateEvent),
            excludeUserId: domainEvent.AggregateEvent.SenderPeerId,
            pts: domainEvent.AggregateEvent.Pts//,
            //layeredData: layeredUpdatesService.GetLayeredData(c =>
            //    c.ToUpdatePinnedMessageUpdates(domainEvent.AggregateEvent))
        );
    }

    public Task HandleAsync(IDomainEvent<PeerNotifySettingsAggregate, PeerNotifySettingsId, PeerNotifySettingsUpdatedEvent> domainEvent, CancellationToken cancellationToken)
    {
        var r = new TBoolTrue();

        return SendRpcMessageToClientAsync(domainEvent.AggregateEvent.RequestInfo, r);
    }

    public Task HandleAsync(IDomainEvent<UserAggregate, UserId, UserGlobalPrivacySettingsChangedEvent> domainEvent, CancellationToken cancellationToken)
    {
        return cacheManager.SetAsync(
            GlobalPrivacySettingsCacheItem.GetCacheKey(domainEvent.AggregateEvent.RequestInfo.UserId),
            new GlobalPrivacySettingsCacheItem(domainEvent.AggregateEvent.GlobalPrivacySettings.ArchiveAndMuteNewNoncontactPeers,
                domainEvent.AggregateEvent.GlobalPrivacySettings.KeepArchivedUnmuted,
                domainEvent.AggregateEvent.GlobalPrivacySettings.KeepArchivedFolders,
                domainEvent.AggregateEvent.GlobalPrivacySettings.HideReadMarks,
                domainEvent.AggregateEvent.GlobalPrivacySettings.NewNoncontactPeersRequirePremium
                )
        );
    }

    public async Task HandleAsync(IDomainEvent<DeleteMessagesSaga4, DeleteMessagesSaga4Id, DeleteSelfHistoryCompletedSagaEvent> domainEvent, CancellationToken cancellationToken)
    {
        var r = new TAffectedHistory
        {
            Pts = domainEvent.AggregateEvent.Pts,
            PtsCount = domainEvent.AggregateEvent.PtsCount,
            Offset = domainEvent.AggregateEvent.Offset
        };
        await SendRpcMessageToClientAsync(domainEvent.AggregateEvent.RequestInfo, r);
        var selfOtherDeviceUpdates = updatesConverterService.ToDeleteMessagesUpdates(PeerType.User,
            new DeletedBoxItem(domainEvent.AggregateEvent.RequestInfo.UserId, domainEvent.AggregateEvent.Pts,
                domainEvent.AggregateEvent.PtsCount, domainEvent.AggregateEvent.MessageIds),
            DateTime.UtcNow.ToTimestamp());

        await PushUpdatesToPeerAsync(domainEvent.AggregateEvent.RequestInfo.UserId.ToUserPeer(), selfOtherDeviceUpdates,
            domainEvent.AggregateEvent.RequestInfo.AuthKeyId);
    }

    public Task HandleAsync(IDomainEvent<DeleteMessagesSaga4, DeleteMessagesSaga4Id, DeleteOtherParticipantHistoryCompletedSagaEvent> domainEvent, CancellationToken cancellationToken)
    {
        var updates = updatesConverterService.ToDeleteMessagesUpdates(PeerType.User,
            new DeletedBoxItem(domainEvent.AggregateEvent.UserId, domainEvent.AggregateEvent.Pts,
                domainEvent.AggregateEvent.PtsCount, domainEvent.AggregateEvent.MessageIds),
            DateTime.UtcNow.ToTimestamp());
        //var layeredUpdates = layeredUpdatesService.GetLayeredData(c => c.ToDeleteMessagesUpdates(PeerType.User,
        //    new DeletedBoxItem(domainEvent.AggregateEvent.UserId, domainEvent.AggregateEvent.Pts,
        //        domainEvent.AggregateEvent.PtsCount, domainEvent.AggregateEvent.MessageIds),
        //    DateTime.UtcNow.ToTimestamp()));

        return PushUpdatesToPeerAsync(domainEvent.AggregateEvent.UserId.ToUserPeer(), updates,
            pts: domainEvent.AggregateEvent.Pts);
    }

    public Task HandleAsync(IDomainEvent<PinForwardedChannelMessageSaga, PinForwardedChannelMessageSagaId, PinChannelMessagePtsIncrementedSagaEvent> domainEvent, CancellationToken cancellationToken)
    {
        var updateShort = new TUpdateShort
        {
            Update = new TUpdatePinnedChannelMessages
            {
                ChannelId = domainEvent.AggregateEvent.ChannelId,
                Messages = [domainEvent.AggregateEvent.MessageId],
                Pinned = true,
                Pts = domainEvent.AggregateEvent.Pts,
                PtsCount = 1
            },
            Date = DateTime.UtcNow.ToTimestamp(),
        };

        return PushMessageToPeerAsync(domainEvent.AggregateEvent.ChannelId.ToChannelPeer(), updateShort);
    }

    public async Task HandleAsync(IDomainEvent<UpdatePinnedMessageSaga, UpdatePinnedMessageSagaId, UpdateSavedMessagesPinnedCompletedSagaEvent> domainEvent, CancellationToken cancellationToken)
    {
        var updatePinnedMessages = new TUpdatePinnedMessages
        {
            Pinned = domainEvent.AggregateEvent.Pinned,
            Peer = new TPeerUser
            {
                UserId = domainEvent.AggregateEvent.RequestInfo.UserId
            },
            Messages = new TVector<int>(domainEvent.AggregateEvent.MessageIds),
            Pts = domainEvent.AggregateEvent.Pts,
            PtsCount = 1
        };
        var updates = new TUpdates
        {
            Updates = new TVector<IUpdate>(updatePinnedMessages),
            Users = new(),
            Chats = new(),
            Date = DateTime.UtcNow.ToTimestamp(),
        };
        await SendRpcMessageToClientAsync(domainEvent.AggregateEvent.RequestInfo, updates,
            pts: domainEvent.AggregateEvent.Pts);

        await PushUpdatesToPeerAsync(domainEvent.AggregateEvent.RequestInfo.UserId.ToUserPeer(), updates,
            domainEvent.AggregateEvent.RequestInfo.PermAuthKeyId, pts: domainEvent.AggregateEvent.Pts);

    }
}
