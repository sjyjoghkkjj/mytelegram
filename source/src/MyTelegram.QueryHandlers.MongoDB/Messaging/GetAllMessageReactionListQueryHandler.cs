namespace MyTelegram.QueryHandlers.MongoDB.Messaging;

public class GetAllMessageReactionListQueryHandler(
    IQueryOnlyReadModelStore<UserReactionReadModel> store
) : IQueryHandler<GetAllMessageReactionListQuery, IReadOnlyCollection<IUserReactionReadModel>>
{
    public async Task<IReadOnlyCollection<IUserReactionReadModel>> ExecuteQueryAsync(GetAllMessageReactionListQuery query, CancellationToken cancellationToken)
    {
        return await store.FindAsync(p => query.Ids.Contains(p.Id), cancellationToken: cancellationToken);
    }
}

