namespace MyTelegram.QueryHandlers.MongoDB.Messaging;

public class GetMessageReactionsQueryHandler(
    IQueryOnlyReadModelStore<UserReactionReadModel> store
) : IQueryHandler<GetMessageReactionsQuery, IReadOnlyCollection<IUserReactionReadModel>>
{
    public async Task<IReadOnlyCollection<IUserReactionReadModel>> ExecuteQueryAsync(GetMessageReactionsQuery query, CancellationToken cancellationToken)
    {
        return await store.FindAsync(p => p.PeerId == query.ToPeerId && query.MessageIds.Contains(p.MessageId), cancellationToken: cancellationToken);
    }
}

