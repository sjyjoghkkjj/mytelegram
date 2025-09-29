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
    IChannelAdminRightsChecker channelAdminRightsChecker
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

        // TODO: Persist available reactions in channel/chat settings and emit proper updates.
        // For now, return empty updates to acknowledge the call.
        return new TUpdates
        {
            Chats = [],
            Users = [],
            Updates = [],
            Date = CurrentDate
        };
    }
}
