namespace MyTelegram.Messenger.Handlers.LatestLayer.Messages;

/// <summary>
/// Get <a href="https://corefork.telegram.org/api/reactions">message reactions »</a>
/// <para>Possible errors</para>
/// Code Type Description
/// 400 CHANNEL_PRIVATE You haven't joined this channel/supergroup.
/// See <a href="https://corefork.telegram.org/method/messages.getMessagesReactions" />
///</summary>
internal sealed class GetMessagesReactionsHandler(
    IQueryProcessor queryProcessor,
    IPeerHelper peerHelper,
    IAccessHashHelper accessHashHelper,
    IMessageReactionsResponseService messageReactionsResponseService
) : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestGetMessagesReactions, MyTelegram.Schema.IUpdates>
{
    protected override async Task<MyTelegram.Schema.IUpdates> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestGetMessagesReactions obj)
    {
        await accessHashHelper.CheckAccessHashAsync(input, obj.Peer);
        var peer = peerHelper.GetPeer(obj.Peer, input.UserId);
        var ownerPeerId = peer.PeerType == PeerType.Channel ? peer.PeerId : input.UserId;

        var messageIds = obj.Id.Select(id => MessageId.Create(ownerPeerId, id).Value).ToList();
        var messages = await queryProcessor.ProcessAsync(new GetMessagesByIdListQuery(messageIds));

        var updates = new List<IUpdate>();
        foreach (var m in messages)
        {
            var results = new TVector<IReactionCount>();
            if (m.Reactions != null)
            {
                foreach (var r in m.Reactions)
                {
                    results.Add(new TReactionCount
                    {
                        Reaction = r.GetReaction(),
                        Count = r.Count
                    });
                }
            }

            var recent = new TVector<IMessagePeerReaction>();
            if (m.RecentReactions2 != null)
            {
                foreach (var rr in m.RecentReactions2)
                {
                    recent.Add(new TMessagePeerReaction
                    {
                        Big = rr.Big,
                        Date = rr.Date,
                        PeerId = rr.PeerId.ToPeer(),
                        Reaction = rr.Reaction
                    });
                }
            }

            var msgReactions = new TMessageReactions
            {
                Results = results,
                RecentReactions = recent.Count > 0 ? recent : null,
                CanSeeList = true
            };

            updates.Add(new TUpdateMessageReactions
            {
                Peer = peer.ToPeer(),
                MsgId = m.MessageId,
                Reactions = messageReactionsResponseService.ToLayeredData(msgReactions, input.Layer) ?? msgReactions
            });
        }

        return new TUpdates
        {
            Updates = new TVector<IUpdate>(updates),
            Chats = [],
            Users = [],
            Date = CurrentDate,
        };
    }
}
