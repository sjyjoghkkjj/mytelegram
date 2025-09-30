namespace MyTelegram.QueryHandlers.MongoDB.Forum;

public class GetForumTopicsByIdsQueryHandler(IQueryOnlyReadModelStore<ForumTopicReadModel> store)
    : IQueryHandler<GetForumTopicsByIdsQuery, IReadOnlyCollection<IForumTopicReadModel>>
{
    public async Task<IReadOnlyCollection<IForumTopicReadModel>> ExecuteQueryAsync(GetForumTopicsByIdsQuery query,
        CancellationToken cancellationToken)
    {
        var ids = query.TopicIds.ToHashSet();
        var result = await store.FindAsync(p => p.ChannelId == query.ChannelId && ids.Contains(p.TopicId), cancellationToken);
        return result.ToList();
    }
}

