namespace MyTelegram.Domain.Aggregates.Channel;

public class ChannelAggregate : MyInMemorySnapshotAggregateRoot<ChannelAggregate, ChannelId, ChannelSnapshot>
{
    private readonly ChannelState _state = new();

    public ChannelAggregate(ChannelId id) : base(id,
        SnapshotEveryFewVersionsStrategy.Default)
    {
        Register(_state);
    }

    public void UpdateParticipantCount(int updatedCount)
    {
        Specs.AggregateIsCreated.ThrowDomainErrorIfNotSatisfied(this);
        var newCount = _state.ParticipantCount + updatedCount;
        Emit(new ChannelParticipantCountChangedEvent(_state.ChannelId, newCount));
    }

    public void DeleteChannel(RequestInfo requestInfo)
    {
        Specs.AggregateIsCreated.ThrowDomainErrorIfNotSatisfied(this);

        if (_state.CreatorId != requestInfo.UserId)
        {
            RpcErrors.RpcErrors400.ChatAdminRequired.ThrowRpcError();
        }

        Emit(new ChannelDeletedEvent(requestInfo, _state.ChannelId, _state.Broadcast, !_state.Broadcast, _state.AccessHash, _state.Title));
    }

    public void CheckChannelState(
        RequestInfo requestInfo,
        long senderPeerId,
        int messageId,
        int date,
        MessageSubType messageSubType)
    {
        Specs.AggregateIsCreated.ThrowDomainErrorIfNotSatisfied(this);

        var admin = _state.GetAdmin(senderPeerId);
        if (_state.Broadcast && _state.CreatorId != senderPeerId)
        {
            if (admin == null || !admin.AdminRights.PostMessages)
            {
                if (messageSubType != MessageSubType.InviteToChannel)
                {
                    //ThrowHelper.ThrowUserFriendlyException(RpcErrorMessages.ChatWriteForbidden);
                    RpcErrors.RpcErrors403.ChatWriteForbidden.ThrowRpcError();
                }
            }
        }

        if (messageSubType != MessageSubType.InviteToChannel)
        {
            if (_state.Broadcast)
            {
                CheckAdminRights(senderPeerId, r => r.PostMessages, RpcErrors.RpcErrors403.ChatWriteForbidden);
            }

            CheckBannedRights(senderPeerId, _state.GetDefaultBannedRights().SendMessages,
                RpcErrors.RpcErrors403.ChatWriteForbidden);
        }

        if (_state.SlowModeSeconds > 0)
        {
            if (senderPeerId == _state.LatestNoneBotSenderPeerId && senderPeerId != _state.CreatorId)
            {
                var nextSendDate = _state.SlowModeSeconds + _state.LastSendDate;
                var now = DateTime.UtcNow.ToTimestamp();
                var waitForX = nextSendDate - now;
                if (waitForX > 0)
                {
                    //ThrowHelper.ThrowUserFriendlyException(string.Format(RpcErrorMessages.SlowModeWait, waitForX));
                    RpcErrors.RpcErrors420.SlowModeWaitX.ThrowRpcError(waitForX);
                }
            }
        }

        Emit(new CheckChannelStateCompletedEvent(
            requestInfo,
            senderPeerId,
            messageId,
            date,
            _state.Broadcast,
            _state.Broadcast ? 1 : 0,
            _state.BotUserIdList,
            _state.LinkedChannelId));
    }

    public void Create(RequestInfo requestInfo,
        long channelId,
        long creatorId,
        bool broadcast,
        bool megaGroup,
        string title,
        string? about,
        GeoPoint? geoPoint,
        string? address,
        long accessHash,
        int date,
        long randomId,
        IMessageAction messageAction,
        int? ttlPeriod,
        bool migratedFromChat,
        long? migratedFromChatId,
        int? migratedMaxId,
        long? photoId,
        bool autoCreateFromChat,
        bool ttlFromDefaultSetting,
        List<long>? memberUserIds,
        List<long>? botUserIds
        )
    {
        Specs.AggregateIsNew.ThrowDomainErrorIfNotSatisfied(this);
        Emit(new ChannelCreatedEvent(requestInfo,
            channelId,
            creatorId,
            title,
            broadcast,
            megaGroup,
            about,
            geoPoint,
            address,
            accessHash,
            date,
            randomId,
            messageAction,
            ttlPeriod,
            migratedFromChat,
            migratedFromChatId,
            migratedMaxId,
            photoId,
            autoCreateFromChat,
            ttlFromDefaultSetting,
            memberUserIds ?? [],
            botUserIds ?? []
        ));
    }

    public void EditAbout(RequestInfo requestInfo,
        long selfUserId,
        string? about)
    {
        Specs.AggregateIsCreated.ThrowDomainErrorIfNotSatisfied(this);
        //var admin = _state.GetAdmin(selfUserId);
        CheckAdminRights(requestInfo, r => r.ChangeInfo);
        CheckBannedRights(requestInfo, _state.GetDefaultBannedRights().ChangeInfo);

        //CheckBannedRights(selfUserId,
        //    _state.GetDefaultBannedRights().ChangeInfo,
        //    admin?.AdminRights.ChangeInfo,
        //    RpcErrorMessages.ChatAdminRequired);

        if (about?.Length > MyTelegramServerDomainConsts.ChatAboutMaxLength)
        {
            //ThrowHelper.ThrowUserFriendlyException(RpcErrorMessages.ChatAboutTooLong);
            RpcErrors.RpcErrors400.ChatAboutTooLong.ThrowRpcError();
        }

        Emit(new ChannelAboutEditedEvent(requestInfo, _state.ChannelId, about));
    }

    public void EditAdmin(RequestInfo requestInfo,
        long promotedBy,
        bool canEdit,
        long userId,
        bool isBot,
        bool isChannelMember,
        ChatAdminRights adminRights,
        string rank,
        int date
    )
    {
        Specs.AggregateIsCreated.ThrowDomainErrorIfNotSatisfied(this);
        if (_state.ChatAdmins.Count > MyTelegramServerDomainConsts.ChannelAdminMaxCount)
        {
            //ThrowHelper.ThrowUserFriendlyException(RpcErrorMessages.AdminsTooMuch);
            RpcErrors.RpcErrors400.AdminsTooMuch.ThrowRpcError();
        }

        CheckAdminRights(requestInfo, r => r.AddAdmins);

        // flags value==0 means no rights(should remove member from admin list)
        var removeFromAdminList = adminRights.GetFlags().ToInt32() == 0;
        var isNewAdmin = !_state.ChatAdmins.ContainsKey(userId);

        Emit(new ChannelAdminRightsEditedEvent(requestInfo,
            _state.ChannelId,
            promotedBy,
            canEdit,
            userId,
            isBot,
            isChannelMember,
            isNewAdmin,
            adminRights,
            rank,
            removeFromAdminList,
            date,
            _state.Broadcast
        ));
    }

    public void EditChatDefaultBannedRights(RequestInfo requestInfo,
        ChatBannedRights bannedRights,
        long selfUserId)
    {
        Specs.AggregateIsCreated.ThrowDomainErrorIfNotSatisfied(this);
        CheckAdminRights(requestInfo, r => r.Other);

        Emit(new ChannelDefaultBannedRightsEditedEvent(requestInfo, _state.ChannelId, bannedRights));
    }

    public void EditPhoto(RequestInfo requestInfo,
        //long fileId,
        //byte[] photo,
        long? photoId,
        IMessageAction messageAction,
        long randomId)
    {
        Specs.AggregateIsCreated.ThrowDomainErrorIfNotSatisfied(this);
        CheckAdminRights(requestInfo, r => r.ChangeInfo);

        Emit(new ChannelPhotoEditedEvent(requestInfo,
            _state.ChannelId,
            _state.Broadcast,
            //photo,
            photoId,
            messageAction,
            randomId
            ));
    }

    public void EditTitle(RequestInfo requestInfo,
        string title,
        IMessageAction messageAction,
        long randomId)
    {
        Specs.AggregateIsCreated.ThrowDomainErrorIfNotSatisfied(this);
        //var admin = _state.GetAdmin(requestInfo.UserId);
        //CheckBannedRights(requestInfo.UserId,
        //    _state.GetDefaultBannedRights().ChangeInfo,
        //    admin?.AdminRights.ChangeInfo,
        //    RpcErrorMessages.ChatAdminRequired);
        CheckAdminRights(requestInfo, r => r.ChangeInfo);

        Emit(new ChannelTitleEditedEvent(requestInfo,
            _state.ChannelId,
            _state.Broadcast,
            title,
            messageAction,
            randomId
            ));
    }

    public void HideChatJoinRequest(RequestInfo requestInfo, long userId, bool approved)
    {
        Specs.AggregateIsCreated.ThrowDomainErrorIfNotSatisfied(this);
        CheckAdminRights(requestInfo, rights => rights.InviteUsers);

        var recentRequesters = _state.RecentRequesters ?? new List<long>();
        var requestsPending = _state.RequestsPending ?? 0;

        if (recentRequesters.Contains(userId))
        {
            recentRequesters.Remove(userId);
            requestsPending--;
        }

        //Emit(new JoinRequestHiddenEvent(requestInfo, _state.ChannelId, inviteId, hash, approved, userId, recentRequesters,
        //    requestsPending, date));

        Emit(new ChatJoinRequestHiddenEvent(requestInfo, _state.ChannelId, _state.Broadcast, userId, approved, requestsPending,
            recentRequesters));
    }

    public void IncrementParticipantCount()
    {
        Specs.AggregateIsCreated.ThrowDomainErrorIfNotSatisfied(this);
        var participantCount = _state.ParticipantCount + 1;
        Emit(new IncrementParticipantCountEvent(_state.ChannelId, participantCount));
    }

    public void ReadChannelLatestNoneBotOutboxMessage(
        RequestInfo requestInfo,
        string sourceCommandId)
    {
        Specs.AggregateIsCreated.ThrowDomainErrorIfNotSatisfied(this);
        Emit(new ReadChannelLatestNoneBotOutboxMessageEvent(
            requestInfo,
            _state.LatestNoneBotSenderPeerId,
            _state.LatestNoneBotSenderMessageId,
            sourceCommandId));
    }

    public void SetDiscussionGroup(RequestInfo requestInfo,
        long broadcastChannelId,
        long? groupChannelId)
    {
        // TODO:Use saga to set discussion group id,check whether the groupChannelId is valid
        Specs.AggregateIsCreated.ThrowDomainErrorIfNotSatisfied(this);
        if (requestInfo.UserId != _state.CreatorId)
        {
            //ThrowHelper.ThrowUserFriendlyException(RpcErrorMessages.BroadcastIdInvalid);
            RpcErrors.RpcErrors400.BroadcastIdInvalid.ThrowRpcError();
        }

        Emit(new DiscussionGroupUpdatedEvent(requestInfo, broadcastChannelId, groupChannelId, _state.LinkedChannelId));
    }

    public void SetLinkedChannelId(RequestInfo requestInfo, long? linkedChannelId)
    {
        Specs.AggregateIsCreated.ThrowDomainErrorIfNotSatisfied(this);
        Emit(new LinkedChannelChangedEvent(requestInfo, _state.ChannelId, linkedChannelId, _state.LinkedChannelId));
    }

    public void SetPinnedMsgId(int pinnedMsgId,
        bool pinned)
    {
        Specs.AggregateIsCreated.ThrowDomainErrorIfNotSatisfied(this);
        Emit(new NewMsgIdPinnedEvent(pinnedMsgId, pinned));
    }

    public void SetPts(long senderPeerId,
        int pts,
        int messageId,
        int date)
    {
        Specs.AggregateIsCreated.ThrowDomainErrorIfNotSatisfied(this);
        Emit(new SetChannelPtsEvent(senderPeerId, pts, messageId, date));
    }

    public void ToggleNoForwards(RequestInfo requestInfo, bool enabled)
    //{
    //    Specs.AggregateIsCreated.ThrowDomainErrorIfNotSatisfied(this);
    //    //var admin = _state.GetAdmin(requestInfo.UserId);
    //    //CheckBannedRights(requestInfo.UserId, true, admin?.AdminRights.DeleteMessages, RpcErrorMessages.ChatAdminRequired);
    //    CheckAdminRights(requestInfo, r => r.DeleteMessages);

    //    Emit(new DeleteParticipantHistoryStartedEvent(requestInfo, _state.ChannelId, messageIds));
    //}

    public void StartInviteToChannel(RequestInfo requestInfo,
        long inviterId,
        int maxMessageId,
        IReadOnlyList<long> memberUserIdList,
        IReadOnlyList<long>? privacyRestrictedUserId,
        IReadOnlyList<long> botUserIdList,
        int date,
        long randomId,
        string messageActionData)
    {
        Specs.AggregateIsCreated.ThrowDomainErrorIfNotSatisfied(this);
        CheckAdminRights(requestInfo, rights => rights.ChangeInfo);
        Emit(new ChannelNoForwardsChangedEvent(requestInfo, _state.ChannelId, enabled));
        if (memberUserIdList.Count == 1 && memberUserIdList.ElementAt(0) == inviterId)
        {
        }
        else
        {
            CheckAdminRights(requestInfo, r => r.InviteUsers);
            CheckBannedRights(requestInfo, _state.GetDefaultBannedRights().InviteUsers);
            //var admin = _state.GetAdmin(inviterId);
            //CheckBannedRights(inviterId,
            //    _state.GetDefaultBannedRights().InviteUsers,
            //    admin?.AdminRights.InviteUsers,
            //    RpcErrorMessages.ChatAdminRequired);
        }

        Emit(new StartInviteToChannelEvent(requestInfo,
            _state.ChannelId,
            inviterId,
            memberUserIdList,
            privacyRestrictedUserId,
            botUserIdList,
            date,
            //_state.MaxMessageId,
            maxMessageId,
            _state.PreHistoryHidden ? maxMessageId : 0,
            randomId,
            messageActionData,
            _state.Broadcast,
            _state.HasLink
        ));
    }

    public void StartSendChannelMessage( //long reqMsgId,
        RequestInfo requestInfo,
        long senderPeerId,
        bool senderIsBot,
        int messageId,
        MessageSubType subType)
    {
        Specs.AggregateIsCreated.ThrowDomainErrorIfNotSatisfied(this);
        if (_state.Broadcast && _state.CreatorId != senderPeerId)
        {
            var admin = _state.GetAdmin(senderPeerId);
            if (admin == null || !admin.AdminRights.PostMessages)
            {
                if (subType != MessageSubType.InviteToChannel)
                {
                    //ThrowHelper.ThrowUserFriendlyException(RpcErrorMessages.ChatWriteForbidden);
                    RpcErrors.RpcErrors403.ChatWriteForbidden.ThrowRpcError();
                }
            }
        }

        if (subType != MessageSubType.InviteToChannel)
        {
            //CheckBannedRights(senderPeerId,
            //    _state.GetDefaultBannedRights().SendMessages,
            //    false,
            //    RpcErrorMessages.ChatWriteForbidden);
            CheckBannedRights(senderPeerId, _state.GetDefaultBannedRights().SendMessages,
                RpcErrors.RpcErrors403.ChatWriteForbidden);
        }

        if (_state.SlowModeSeconds > 0)
        {
            if (senderPeerId == _state.LatestNoneBotSenderPeerId && senderPeerId != _state.CreatorId)
            {
                var nextSendDate = _state.SlowModeSeconds + _state.LastSendDate;
                var now = DateTime.UtcNow.ToTimestamp();
                var waitForX = nextSendDate - now;
                if (waitForX > 0)
                {
                    //ThrowHelper.ThrowUserFriendlyException(string.Format(RpcErrorMessages.SlowModeWait, waitForX));
                    RpcErrors.RpcErrors420.SlowModeWaitX.ThrowRpcError(waitForX);
                }
            }
        }

        Emit(new StartSendChannelMessageEvent( //reqMsgId,
            requestInfo,
            senderPeerId,
            senderIsBot,
            _state.Broadcast,
            _state.Broadcast ? 1 : 0,
            messageId,
            _state.BotUserIdList,
            _state.LinkedChannelId,
            DateTime.UtcNow.ToTimestamp()
        ));
    }

    public void TogglePreHistoryHidden(RequestInfo requestInfo,
        bool hidden,
        long selfUserId)
    {
        Specs.AggregateIsCreated.ThrowDomainErrorIfNotSatisfied(this);
        // only channel creator can change this setting
        //CheckBannedRights(selfUserId, true, false, RpcErrorMessages.ChatAdminRequired);
        CheckAdminRights(selfUserId, r => false, RpcErrors.RpcErrors400.ChatAdminRequired);
        Emit(new PreHistoryHiddenChangedEvent(requestInfo, _state.ChannelId, hidden));
    }

    public void ToggleSignature(RequestInfo requestInfo, bool signatureEnabled, bool profilesEnabled)
    {
        Specs.AggregateIsCreated.ThrowDomainErrorIfNotSatisfied(this);
        CheckAdminRights(requestInfo, rights => rights.ChangeInfo);
        Emit(new ChannelSignatureChangedEvent(requestInfo, _state.ChannelId, signatureEnabled, profilesEnabled));
    }
    public void ToggleSlowMode(RequestInfo requestInfo,
        int seconds,
        long selfUserId)
    {
        Specs.AggregateIsCreated.ThrowDomainErrorIfNotSatisfied(this);
        //CheckBannedRights(selfUserId,
        //    _state.GetDefaultBannedRights().ChangeInfo,
        //    false,
        //    RpcErrorMessages.ChatAdminRequired);
        CheckBannedRights(selfUserId, _state.GetDefaultBannedRights().ChangeInfo,
            RpcErrors.RpcErrors400.ChatAdminRequired);

        Emit(new SlowModeChangedEvent(requestInfo, _state.ChannelId, seconds));
    }

    //        if (bannedRights)
    //        {
    //            ThrowHelper.ThrowUserFriendlyException(error);
    //        }
    //    }
    //}
    public void UpdateChatInviteRequestPending(long requestUserId)
    {
        Specs.AggregateIsCreated.ThrowDomainErrorIfNotSatisfied(this);
        var recentRequesters = _state.RecentRequesters ?? new List<long>();
        var requestsPending = _state.RequestsPending;
        if (!recentRequesters.Contains(requestUserId))
        {
            recentRequesters.Add(requestUserId);

            if (recentRequesters.Count > MyTelegramServerDomainConsts.ChatInviteRecentRequesterMaxCount)
            {
                recentRequesters.RemoveAt(0);
            }

            requestsPending ??= 0;
            requestsPending++;

            Emit(new ChatInviteRequestPendingUpdatedEvent(_state.ChannelId,
                _state.ChatAdmins.Select(p => p.Key).ToList(), recentRequesters, requestsPending));
        }
    }

    public void UpdateColor(RequestInfo requestInfo, PeerColor color, long? backgroundEmojiId, bool forProfile)
    {
        Specs.AggregateIsCreated.ThrowDomainErrorIfNotSatisfied(this);
        CheckAdminRights(requestInfo, rights => rights.ChangeInfo);
        Emit(new ChannelColorUpdatedEvent(requestInfo, _state.ChannelId, color, backgroundEmojiId, forProfile));
    }
    public void UpdateUserName(RequestInfo requestInfo,
            string userName)
    {
        Specs.AggregateIsCreated.ThrowDomainErrorIfNotSatisfied(this);
        Emit(new ChannelUserNameChangedEvent(requestInfo,
            _state.ChannelId,
            userName,
            _state.UserName));
    }

    //{
    //    if (_state.CreatorId != selfUserId)
    //    {
    //        if (adminRights.HasValue && adminRights.Value)
    //        {
    //            return;
    //        }
    protected override Task<ChannelSnapshot> CreateSnapshotAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new ChannelSnapshot(_state.Broadcast,
            _state.ChannelId,
            _state.AccessHash,
            _state.Title,
            _state.CreatorId,
            _state.PhotoId,
            _state.PreHistoryHidden,
            _state.MaxMessageId,
            _state.BotUserIdList,
            _state.LatestNoneBotSenderPeerId,
            _state.LatestNoneBotSenderMessageId,
            _state.DefaultBannedRights,
            _state.SlowModeSeconds,
            _state.LastSendDate,
            //_state.LastSenderPeerId,
            _state.ChatAdmins.Select(p => p.Value).ToList(),
            _state.PinnedMsgId,
            _state.Photo,
            _state.LinkedChannelId,
            _state.UserName,
            _state.Forum,
            _state.MaxTopicId,
            _state.TtlPeriod,
            _state.MigratedFromChatId,
            _state.MigratedMaxId,
            _state.NoForwards,
            _state.IsFirstChatInviteCreated,
            _state.RequestsPending,
            _state.RecentRequesters?.ToList() ?? [],
            _state.SignatureEnabled,
            _state.ParticipantCount,
            _state.Color,
            _state.HasLink,
            _state.IsDeleted,
            _state.WallPaperId,
            _state.ThemeEmoticon,
            _state.WallPaperSettings,
            _state.IsGeoGroup,
            _state.TopMessageId,
            _state.StickerSetId,
            _state.EmojiStickerSetId,
            _state.EmojiStatus
        ));
    }
    protected override Task LoadSnapshotAsync(ChannelSnapshot snapshot,
        ISnapshotMetadata metadata,
        CancellationToken cancellationToken)
    {
        _state.LoadFromSnapshot(snapshot);
        return Task.CompletedTask;
    }
    private void CheckAdminRights(RequestInfo requestInfo, Func<ChatAdminRights, bool> rightsToCheck,
        RpcError? rpcError = null /*string errorMessage = RpcErrorMessages.ChatAdminRequired*/)
    {
        CheckAdminRights(requestInfo.UserId, rightsToCheck, rpcError);
    }

    private void CheckAdminRights(long userId, Func<ChatAdminRights, bool> rightsToCheck,
        RpcError? rpcError = null /*string errorMessage = RpcErrorMessages.ChatAdminRequired*/)
    {
        if (_state.IsDeleted)
        {
            RpcErrors.RpcErrors400.ChannelPrivate.ThrowRpcError();
        }

        if (_state.CreatorId != userId)
        {
            var admin = _state.GetAdmin(userId);

            if (admin == null)
            {
                //ThrowHelper.ThrowUserFriendlyException(errorMessage);
                (rpcError ?? RpcErrors.RpcErrors400.ChatAdminRequired).ThrowRpcError();
            }

            if (admin!.UserId != _state.CreatorId)
            {
                var rights = rightsToCheck(admin.AdminRights);
                if (!rights)
                {
                    //ThrowHelper.ThrowUserFriendlyException(errorMessage);
                    (rpcError ?? RpcErrors.RpcErrors400.ChatAdminRequired).ThrowRpcError();
                }
            }
        }
    }

    private void CheckBannedRights(RequestInfo requestInfo, bool bannedRights, RpcError? rpcError = null)
    {
        if (_state.IsDeleted)
        {
            RpcErrors.RpcErrors400.ChannelPrivate.ThrowRpcError();
        }

        CheckBannedRights(requestInfo.UserId, bannedRights, rpcError);
    }

    private void CheckBannedRights(long userId, bool bannedRights,
        RpcError? rpcError = null /*string errorMessage = RpcErrorMessages.ChatAdminRequired*/)
    {
        if (_state.CreatorId != userId)
        {
            if (bannedRights)
            {
                //ThrowHelper.ThrowUserFriendlyException(errorMessage);
                (rpcError ?? RpcErrors.RpcErrors400.ChatAdminRequired).ThrowRpcError();
            }
        }
    }
}