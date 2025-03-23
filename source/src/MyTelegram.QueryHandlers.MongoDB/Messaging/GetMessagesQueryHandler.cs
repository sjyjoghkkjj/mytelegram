namespace MyTelegram.QueryHandlers.MongoDB.Messaging;

// ReSharper disable once UnusedMember.Global
public class
    GetMessagesQueryHandler(IQueryOnlyReadModelStore<MessageReadModel> store) : IQueryHandler<GetMessagesQuery, IReadOnlyCollection<IMessageReadModel>>
{
    public async Task<IReadOnlyCollection<IMessageReadModel>> ExecuteQueryAsync(GetMessagesQuery query,
        CancellationToken cancellationToken)
    {
        //var filter = Builders<MessageReadModel>.Filter.Where(p => p.Out);
        Expression<Func<MessageReadModel, bool>> predicate;
        if (query.IsSearchGlobal)
        {
            predicate = x => x.OwnerPeerId == query.OwnerPeerId;
            if (query.JoinedChannelIdList?.Count > 0)
            {
                predicate = predicate.Or(p => query.JoinedChannelIdList.Contains(p.OwnerPeerId));
            }
        }
        else
        {
            predicate = x => x.OwnerPeerId == query.OwnerPeerId;
        }

        predicate = predicate
                .WhereIf(query.MessageActionType == null, p => p.MessageActionType != MessageActionType.CreateQuickReplyMessage)
                .WhereIf(query.MessageActionType.HasValue, p => p.MessageActionType == query.MessageActionType)
                //.WhereIf(query.Q?.Length > 2, p => p.Message.Contains(query.Q!))
                .WhereIf(query.Q?.Length > 0, p => p.Message.Contains(query.Q!))
                .WhereIf(
                    query.MessageType != MessageType.Unknown && query.MessageType != MessageType.Pinned,
                    p => p.MessageType == query.MessageType)
                .WhereIf(query.MessageType == MessageType.Pinned, p => p.Pinned)
                .WhereIf(query.MessageIdList?.Count > 0, p => query.MessageIdList!.Contains(p.MessageId))
                .WhereIf(query.ChannelHistoryMinId > 0, p => p.MessageId > query.ChannelHistoryMinId)
                //.WhereIf(query.Offset?.LoadType == LoadType.Forward, p => p.MessageId > query.Offset!.FromId)
                //.WhereIf(query.Offset?.MaxId > 0, p => p.MessageId < query.Offset!.MaxId)
                .WhereIf(query.Offset is { LoadType: LoadType.Backward, MaxId: > 0 }, p => p.MessageId < query.Offset!.MaxId)
                .WhereIf(query.Offset is { LoadType: LoadType.AroundMessage, MaxId: > 0 }, p => p.MessageId < query.Offset!.MaxId)
                .WhereIf(query.Offset?.LoadType == LoadType.Forward, p => p.MessageId > query.Offset!.FromId)
                .WhereIf(query.Pts > 0, p => p.Pts > query.Pts)
                .WhereIf(query.Peer != null && query.Peer.PeerType != PeerType.Empty,
                    p => p.ToPeerType == query.Peer!.PeerType && p.ToPeerId == query.Peer.PeerId)
                .WhereIf(query.ReplyToMsgId > 0, p => p.ReplyToMsgId == query.ReplyToMsgId)
                .WhereIf(query.BroadcastsOnly, p => p.ToPeerType == PeerType.Channel && p.Post)
                .WhereIf(query.GroupsOnly, p => p.ToPeerType == PeerType.Channel && !p.Post)
                .WhereIf(query.UsersOnly, p => p.ToPeerType == PeerType.User)
            ;

        var sortOptions = new SortOptions<MessageReadModel>(p => p.MessageId, SortType.Descending);
        if (query.Offset?.LoadType == LoadType.Forward)
        {
            sortOptions = new(p => p.MessageId, SortType.Ascending);
        }

        var result= await store.FindAsync(predicate,
               0,
               query.Limit,
               sort: sortOptions,
               cancellationToken: cancellationToken);

        return result.OrderByDescending(p => p.MessageId).ToList();
    }
}
