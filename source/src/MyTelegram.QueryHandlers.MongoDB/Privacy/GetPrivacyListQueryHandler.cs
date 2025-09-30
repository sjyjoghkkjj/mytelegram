namespace MyTelegram.QueryHandlers.MongoDB.Privacy;

public class GetPrivacyListQueryHandler(IQueryOnlyReadModelStore<PrivacyReadModel> store)
    : IQueryHandler<GetPrivacyListQuery, IReadOnlyCollection<IPrivacyReadModel>>
{
    public async Task<IReadOnlyCollection<IPrivacyReadModel>> ExecuteQueryAsync(GetPrivacyListQuery query,
        CancellationToken cancellationToken)
    {
        var typeSet = query.PrivacyTypes.ToHashSet();
        var result = await store.FindAsync(p => query.UserIdList.Contains(p.UserId) && typeSet.Contains(p.PrivacyType), cancellationToken);
        return result.ToList();
    }
}

