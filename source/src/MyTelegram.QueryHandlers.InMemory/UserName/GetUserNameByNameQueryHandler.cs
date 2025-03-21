namespace MyTelegram.QueryHandlers.InMemory.UserName;

public class
    GetUserNameByNameQueryHandler(IQueryOnlyReadModelStore<UserNameReadModel> store) : IQueryHandler<GetUserNameByNameQuery, IUserNameReadModel?>
{
    public async Task<IUserNameReadModel?> ExecuteQueryAsync(GetUserNameByNameQuery query,
        CancellationToken cancellationToken)
    {
        return await store.FirstOrDefaultAsync(p => p.UserName == query.Name, cancellationToken);
    }
}
