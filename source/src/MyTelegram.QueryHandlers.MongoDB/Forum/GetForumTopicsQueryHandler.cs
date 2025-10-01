namespace MyTelegram.QueryHandlers.MongoDB.Forum;

public class GetForumTopicsQueryHandler(IQueryOnlyReadModelStore<ForumTopicReadModel> store)
    : IQueryHandler<GetForumTopicsQuery, IReadOnlyCollection<IForumTopicReadModel>>
{
    public async Task<IReadOnlyCollection<IForumTopicReadModel>> ExecuteQueryAsync(GetForumTopicsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await store.FindAsync(p => p.ChannelId == query.ChannelId, cancellationToken);
        return result.OrderByDescending(t => t.Pinned).ThenByDescending(t => t.Date).Take(query.Limit).ToList();
    }
}

