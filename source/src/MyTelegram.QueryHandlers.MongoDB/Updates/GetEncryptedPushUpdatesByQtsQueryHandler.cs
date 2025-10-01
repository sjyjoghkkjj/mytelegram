namespace MyTelegram.QueryHandlers.MongoDB.Updates;

public class GetEncryptedPushUpdatesByQtsQueryHandler(IMongoDbReadModelStore<EncryptedPushUpdatesReadModel> store)
    : IQueryHandler<GetEncryptedPushUpdatesByQtsQuery, IReadOnlyCollection<IEncryptedPushUpdatesReadModel>>
{
    public async Task<IReadOnlyCollection<IEncryptedPushUpdatesReadModel>> ExecuteQueryAsync(GetEncryptedPushUpdatesByQtsQuery query, CancellationToken cancellationToken)
    {
        var result = await store.FindAsync(p => p.InboxOwnerPeerId == query.PeerId && p.InboxOwnerPermAuthKeyId == query.PermAuthKeyId && p.Qts > query.Qts, cancellationToken: cancellationToken);
        return result.Items.OrderBy(p => p.Qts).Take(query.Limit).ToList();
    }
}