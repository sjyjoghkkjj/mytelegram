namespace MyTelegram.QueryHandlers.InMemory.UserName;

public class GetAllUserNameQueryHandler(IQueryOnlyReadModelStore<UserNameReadModel> store) : IQueryHandler<GetAllUserNameQuery, IReadOnlyCollection<string>>
{
    public async Task<IReadOnlyCollection<string>> ExecuteQueryAsync(GetAllUserNameQuery query,
        CancellationToken cancellationToken)
    {
        return await store.FindAsync(p => true, p => p.UserName, query.Skip, query.Limit, cancellationToken: cancellationToken);
    }
}
