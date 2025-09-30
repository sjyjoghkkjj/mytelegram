namespace MyTelegram.QueryHandlers.MongoDB.Forum;

public class GetForumTopicByIdQueryHandler(IQueryOnlyReadModelStore<ForumTopicReadModel> store)
    : IQueryHandler<GetForumTopicByIdQuery, IForumTopicReadModel?>
{
    public async Task<IForumTopicReadModel?> ExecuteQueryAsync(GetForumTopicByIdQuery query,
        CancellationToken cancellationToken)
    {
        return await store.FirstOrDefaultAsync(p => p.ChannelId == query.ChannelId && p.TopicId == query.TopicId, cancellationToken);
    }
}

