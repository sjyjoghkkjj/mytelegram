namespace MyTelegram.QueryHandlers.MongoDB.Channel;

public class GetCommonChatCountQueryHandler
    (IQueryOnlyReadModelStore<ChannelMemberReadModel> store,
        IQueryOnlyReadModelStore<ChannelReadModel> channelStore) : IQueryHandler<GetCommonChatCountQuery, int>
{
    public async Task<int> ExecuteQueryAsync(GetCommonChatCountQuery query,
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
                return 0;
            }

            return (int)await store.CountAsync(p =>
                superGroupIds.Contains(p.ChannelId) && p.UserId == query.TargetUserId, cancellationToken);
        }

        return 0;
    }
}