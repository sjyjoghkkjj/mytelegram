namespace MyTelegram.QueryHandlers.MongoDB.Privacy;

public class GetPrivacyQueryHandler(IQueryOnlyReadModelStore<PrivacyReadModel> store)
    : IQueryHandler<GetPrivacyQuery, IPrivacyReadModel?>
{
    public async Task<IPrivacyReadModel?> ExecuteQueryAsync(GetPrivacyQuery query,
        CancellationToken cancellationToken)
    {
        return await store.FirstOrDefaultAsync(p => p.UserId == query.UserId && p.PrivacyType == query.PrivacyType, cancellationToken);
    }
}

