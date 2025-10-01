namespace MyTelegram.Messenger.Handlers.LatestLayer.Messages;

/// <summary>
/// Get <a href="https://corefork.telegram.org/api/reactions">message reaction</a> list, along with the sender of each reaction.
/// <para>Possible errors</para>
/// Code Type Description
/// 403 BROADCAST_FORBIDDEN Participants of polls in channels should stay anonymous.
/// 400 MSG_ID_INVALID Invalid message ID provided.
/// See <a href="https://corefork.telegram.org/method/messages.getMessageReactionsList" />
///</summary>
internal sealed class GetMessageReactionsListHandler(
    IQueryProcessor queryProcessor,
    IPeerHelper peerHelper,
    IAccessHashHelper accessHashHelper
) : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestGetMessageReactionsList, MyTelegram.Schema.Messages.IMessageReactionsList>
{
    protected override async Task<MyTelegram.Schema.Messages.IMessageReactionsList> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestGetMessageReactionsList obj)
    {
        await accessHashHelper.CheckAccessHashAsync(input, obj.Peer);
        var peer = peerHelper.GetPeer(obj.Peer, input.UserId);
        var ownerPeerId = peer.PeerType == PeerType.Channel ? peer.PeerId : input.UserId;

        var message = await queryProcessor.ProcessAsync(new GetMessageByPeerIdAndMessageIdQuery(ownerPeerId, obj.Id));
        if (message == null)
        {
            RpcErrors.RpcErrors400.MessageIdInvalid.ThrowRpcError();
        }

        var list = new List<IMessagePeerReaction>();
        if (message!.RecentReactions2 != null)
        {
            foreach (var rr in message.RecentReactions2)
            {
                if (obj.Reaction != null && rr.Reaction.GetReactionId() != obj.Reaction.GetReactionId())
                {
                    continue;
                }
                list.Add(new TMessagePeerReaction
                {
                    Big = rr.Big,
                    Date = rr.Date,
                    PeerId = rr.PeerId.ToPeer(),
                    Reaction = rr.Reaction
                });
            }
        }

        // Simple paging using offset and limit
        var skip = Math.Max(obj.Offset, 0);
        var take = Math.Clamp(obj.Limit, 1, 100);
        var sliced = list.Skip(skip).Take(take).ToList();
        var nextOffset = skip + sliced.Count < list.Count ? (skip + sliced.Count).ToString() : null;

        return new TMessageReactionsList
        {
            Count = list.Count,
            Reactions = new TVector<IMessagePeerReaction>(sliced),
            Chats = [],
            Users = [],
            NextOffset = nextOffset
        };
    }
}
