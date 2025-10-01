namespace MyTelegram.Messenger.Handlers.LatestLayer.Messages;

/// <summary>
/// React to message.Starting from layer 159, the reaction will be sent from the peer specified using <a href="https://corefork.telegram.org/method/messages.saveDefaultSendAs">messages.saveDefaultSendAs</a>.
/// <para>Possible errors</para>
/// Code Type Description
/// 400 CHANNEL_INVALID The provided channel is invalid.
/// 400 CHANNEL_PRIVATE You haven't joined this channel/supergroup.
/// 403 CHAT_WRITE_FORBIDDEN You can't write in this chat.
/// 400 MESSAGE_ID_INVALID The provided message id is invalid.
/// 400 MESSAGE_NOT_MODIFIED The provided message data is identical to the previous message data, the message wasn't modified.
/// 400 MSG_ID_INVALID Invalid message ID provided.
/// 400 PEER_ID_INVALID The provided peer id is invalid.
/// 403 PREMIUM_ACCOUNT_REQUIRED A premium account is required to execute this action.
/// 400 REACTIONS_TOO_MANY The message already has exactly <code>reactions_uniq_max</code> reaction emojis, you can't react with a new emoji, see <a href="https://corefork.telegram.org/api/config#client-configuration">the docs for more info »</a>.
/// 400 REACTION_EMPTY Empty reaction provided.
/// 400 REACTION_INVALID The specified reaction is invalid.
/// 400 USER_BANNED_IN_CHANNEL You're banned from sending messages in supergroups/channels.
/// See <a href="https://corefork.telegram.org/method/messages.sendReaction" />
///</summary>
internal sealed class SendReactionHandler(
    ICommandBus commandBus,
    IPeerHelper peerHelper,
    IAccessHashHelper accessHashHelper,
    IQueryProcessor queryProcessor
    ) : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestSendReaction, MyTelegram.Schema.IUpdates>
{
    protected override async Task<MyTelegram.Schema.IUpdates> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestSendReaction obj)
    {
        await accessHashHelper.CheckAccessHashAsync(input, obj.Peer);

        var peer = peerHelper.GetPeer(obj.Peer, input.UserId);
        var ownerPeerId = peer.PeerId;
        if (peer.PeerType != PeerType.Channel)
        {
            ownerPeerId = input.UserId;
        }

        var reactions = obj.Reaction ?? [];
        if (reactions.Count == 0)
        {
            RpcErrors.RpcErrors400.ReactionEmpty.ThrowRpcError();
        }

        var first = reactions[0];

        var message = await queryProcessor.ProcessAsync(new GetMessageByIdQuery(MessageId.Create(ownerPeerId, obj.MsgId).Value));
        if (message == null)
        {
            RpcErrors.RpcErrors400.MessageIdInvalid.ThrowRpcError();
        }

        // validate unique reactions max
        var maxUniq = 2; // default; could be taken from config
        var rm = await queryProcessor.ProcessAsync(new GetMessageByPeerIdAndMessageIdQuery(ownerPeerId, obj.MsgId));
        if (rm?.Reactions != null)
        {
            var uniq = rm.Reactions.Count;
            var newId = first.GetReactionId();
            if (first is not TReactionEmpty && rm.Reactions.All(r => r.GetReactionId() != newId) && uniq >= maxUniq)
            {
                RpcErrors.RpcErrors400.ReactionsTooMany.ThrowRpcError();
            }
        }

        if (first is TReactionEmpty)
        {
            var removeCmd = new RemoveReactionCommand(
                MessageId.Create(ownerPeerId, obj.MsgId),
                input.ToRequestInfo(),
                input.UserId,
                first);
            await commandBus.PublishAsync(removeCmd);
        }
        else
        {
            var sendCmd = new SendReactionCommand(
                MessageId.Create(ownerPeerId, obj.MsgId),
                input.ToRequestInfo(),
                input.UserId,
                first,
                obj.AddToRecent);
            await commandBus.PublishAsync(sendCmd);
        }

        return null!;
    }
}
