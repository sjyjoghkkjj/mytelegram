namespace MyTelegram.QueryHandlers.MongoDB.Messaging;

public class GetMessageReactionsListQueryHandler(
    IQueryOnlyReadModelStore<UserReactionReadModel> store
) : IQueryHandler<GetMessageReactionsListQuery, IReadOnlyCollection<IUserReactionReadModel>>
{
    public async Task<IReadOnlyCollection<IUserReactionReadModel>> ExecuteQueryAsync(GetMessageReactionsListQuery query, CancellationToken cancellationToken)
    {
        var peerId = query.ToPeer.PeerId;
        var reactionId = query.ReactionId;
        return await store.FindAsync(p => p.PeerId == peerId && p.MessageId == query.MessageId && (!reactionId.HasValue || p.ReactionId == reactionId.Value), cancellationToken: cancellationToken);
    }
}

