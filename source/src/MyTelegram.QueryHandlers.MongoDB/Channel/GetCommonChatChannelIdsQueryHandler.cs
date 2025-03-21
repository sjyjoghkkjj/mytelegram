namespace MyTelegram.QueryHandlers.MongoDB.Channel;

public class GetCommonChatChannelIdsQueryHandler
    (IQueryOnlyReadModelStore<ChannelMemberReadModel> store, IQueryOnlyReadModelStore<ChannelReadModel> channelStore) : IQueryHandler<GetCommonChatChannelIdsQuery, IReadOnlyCollection<long>>
{
    public async Task<IReadOnlyCollection<long>> ExecuteQueryAsync(GetCommonChatChannelIdsQuery query,
        CancellationToken cancellationToken)
    {
        var joinedChannelIds = await store.FindAsync(p => p.UserId == query.SelfUserId, p => p.ChannelId, cancellationToken: cancellationToken);
        if (joinedChannelIds.Count > 0)
        {
            // Exclude broadcast channel ids
            var superGroupIds = await channelStore.FindAsync(p => joinedChannelIds.Contains(p.ChannelId) && !p.Broadcast,
                p => p.ChannelId, cancellationToken: cancellationToken);

            if (superGroupIds.Count == 0)
            {
                return [];
            }

            Expression<Func<ChannelMemberReadModel, bool>> predicate = p => superGroupIds.Contains(p.ChannelId) && p.UserId == query.TargetUserId;
            if (query.MaxId > 0)
            {
                predicate = predicate.And(p => p.ChannelId > query.MaxId);
            }

            var channelIds = await store.FindAsync(predicate, p => p.ChannelId,
                limit: query.Limit,
                cancellationToken: cancellationToken);

            return channelIds;
        }

        return [];
    }
}