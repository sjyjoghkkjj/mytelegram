namespace MyTelegram.Messenger.Handlers.LatestLayer.Messages;

/// <summary>
/// Change the set of <a href="https://corefork.telegram.org/api/reactions">message reactions »</a> that can be used in a certain group, supergroup or channel
/// <para>Possible errors</para>
/// Code Type Description
/// 400 CHAT_ADMIN_REQUIRED You must be an admin in this chat to do this.
/// 400 CHAT_NOT_MODIFIED No changes were made to chat information because the new information you passed is identical to the current information.
/// 400 PEER_ID_INVALID The provided peer id is invalid.
/// See <a href="https://corefork.telegram.org/method/messages.setChatAvailableReactions" />
///</summary>
internal sealed class SetChatAvailableReactionsHandler(
    IAccessHashHelper accessHashHelper,
    IPeerHelper peerHelper,
    IChannelAdminRightsChecker channelAdminRightsChecker,
    ICommandBus commandBus
) : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestSetChatAvailableReactions, MyTelegram.Schema.IUpdates>
{
    protected override async Task<MyTelegram.Schema.IUpdates> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestSetChatAvailableReactions obj)
    {
        await accessHashHelper.CheckAccessHashAsync(input, obj.Peer);
        var peer = peerHelper.GetPeer(obj.Peer, input.UserId);
        if (peer.PeerType != PeerType.Channel && peer.PeerType != PeerType.Chat)
        {
            RpcErrors.RpcErrors400.PeerIdInvalid.ThrowRpcError();
        }

        if (peer.PeerType == PeerType.Channel)
        {
            await channelAdminRightsChecker.CheckAdminRightAsync(peer.PeerId, input.UserId,
                p => p.AdminRights.ChangeInfo,
                RpcErrors.RpcErrors400.ChatAdminRequired);
        }

        var (reactionType, allowCustom, list) = Map(obj.AvailableReactions);

        var cmd = new ChangeAvailableReactionsCommand(ChannelId.Create(peer.PeerId), input.ToRequestInfo(), reactionType, allowCustom, list);
        await commandBus.PublishAsync(cmd);

        return null!;
    }

    private static (ReactionType reactionType, bool allowCustom, List<string>? list) Map(IChatReactions chatReactions)
    {
        switch (chatReactions)
        {
            case TChatReactionsNone:
                return (ReactionType.ReactionNone, false, null);
            case TChatReactionsAll tAll:
                return (ReactionType.ReactionAll, tAll.AllowCustom, null);
            case TChatReactionsSome tSome:
                return (ReactionType.ReactionSome, false, tSome.Reactions.Select(r => r switch
                {
                    TReactionEmoji e => e.Emoticon,
                    _ => string.Empty
                }).Where(s => !string.IsNullOrEmpty(s)).ToList());
            default:
                return (ReactionType.ReactionAll, false, null);
        }
    }
}
