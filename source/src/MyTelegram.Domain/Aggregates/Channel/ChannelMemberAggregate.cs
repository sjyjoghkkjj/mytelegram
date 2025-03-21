namespace MyTelegram.Domain.Aggregates.Channel;

public class ChannelMemberAggregate : SnapshotAggregateRoot<ChannelMemberAggregate, ChannelMemberId, ChannelMemberSnapshot>
{
    private readonly ChannelMemberState _state = new();

    public ChannelMemberAggregate(ChannelMemberId id) : base(id, SnapshotEveryFewVersionsStrategy.Default)
    {
        Register(_state);
    }

    public void Create(
        RequestInfo requestInfo,
        long channelId,
        long userId,
        long inviterId,
        int date,
        bool isBot,
        long? chatInviteId,
        ChatJoinType chatJoinType,
        bool isBroadcast
        )
    {
        // Kicked user can not join channel by invite link
        if (_state.KickedBy != 0 && userId == inviterId)
        {
            //ThrowHelper.ThrowUserFriendlyException(RpcErrorMessages.ChannelPrivate);
            RpcErrors.RpcErrors400.ChannelPrivate.ThrowRpcError();
        }

        if (!IsNew && !_state.Left)
        {
            //ThrowHelper.ThrowUserFriendlyException(RpcErrorMessages.UserAlreadyParticipant);
            RpcErrors.RpcErrors400.UserAlreadyParticipant.ThrowRpcError();
        }

        Emit(new ChannelMemberCreatedEvent(
            requestInfo,
            channelId,
            userId,
            inviterId,
            date,
            !IsNew,
            _state.BannedRights,
            isBot,
            chatInviteId,
            chatJoinType,
            isBroadcast
            ));
    }

    public void CreateCreator(RequestInfo requestInfo,
        long channelId,
        long userId,
        int date,
        bool isBroadcast
        )
    {
        Specs.AggregateIsNew.ThrowDomainErrorIfNotSatisfied(this);
        Emit(new ChannelCreatorCreatedEvent(requestInfo,
            channelId,
            userId,
            userId,
            date,
            isBroadcast));
    }

    public void EditBanned(RequestInfo requestInfo,
        long adminId,
        long channelId,
        long memberUserId,
        ChatBannedRights bannedRights)
    {
        Specs.AggregateIsCreated.ThrowDomainErrorIfNotSatisfied(this);

        bool kicked;
        long kickedBy;
        bool left;
        bool removedFromKicked = false;
        bool removedFromBanned = false;
        // User is banned all rights
        if (bannedRights.ViewMessages)
        {
            kicked = true;
            kickedBy = adminId;
            left = true;
        }
        else
        {
            kicked = false;
            kickedBy = 0;
            left = false;
        }

        if (_state.BannedRights != null)
        {
            if (_state.BannedRights.ViewMessages && !bannedRights.ViewMessages)
            {
                removedFromKicked = true;
            }
            else if (bannedRights.ToIntValue() == ChatBannedRights.CreateDefaultBannedRights().ToIntValue())
            {
                removedFromBanned = true;
            }
        }

        var banned = bannedRights.ToIntValue() != ChatBannedRights.CreateDefaultBannedRights().ToIntValue();

        Emit(new ChannelMemberBannedRightsChangedEvent(requestInfo,
            adminId,
            channelId,
            memberUserId,
            _state.IsBot,
            kicked,
            kickedBy,
            left,
            banned,
            removedFromKicked,
            removedFromBanned,
            bannedRights));
    }

    public void Join(RequestInfo requestInfo,
        long channelId,
        long memberUserId,
        bool isBroadcast
        )
    {
        if (_state.KickedBy != 0)
        {
            RpcErrors.RpcErrors400.ChannelPrivate.ThrowRpcError();
        }

        if (!IsNew && !_state.Kicked)
        {
            RpcErrors.RpcErrors400.UserAlreadyParticipant.ThrowRpcError();
        }

        var isBot = memberUserId >= MyTelegramServerDomainConsts.BotUserInitId;

        Emit(new ChannelMemberJoinedEvent(requestInfo,
            channelId,
            memberUserId,
            DateTime.UtcNow.ToTimestamp(),
            !IsNew,
            isBot,
            isBroadcast
            ));
    }

    public void LeaveChannel(RequestInfo requestInfo,
        long channelId,
        long memberUserId)
    {
        Specs.AggregateIsCreated.ThrowDomainErrorIfNotSatisfied(this);
        Emit(new ChannelMemberLeftEvent(requestInfo, channelId, memberUserId, _state.IsBot));
    }

    protected override Task<ChannelMemberSnapshot> CreateSnapshotAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new ChannelMemberSnapshot(_state.Banned, _state.BannedRights, _state.Kicked,
            _state.KickedBy, _state.Left, _state.IsBot));
    }

    protected override Task LoadSnapshotAsync(ChannelMemberSnapshot snapshot, ISnapshotMetadata metadata, CancellationToken cancellationToken)
    {
        _state.LoadSnapshot(snapshot);
        return Task.CompletedTask;
    }
}
