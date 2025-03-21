namespace MyTelegram.QueryHandlers.InMemory.User;

public class GetUserNameByUserIdQueryHandler(IQueryOnlyReadModelStore<UserReadModel> store) : IQueryHandler<GetUserNameByUserIdQuery, string?>
{
    public async Task<string?> ExecuteQueryAsync(GetUserNameByUserIdQuery query,
        CancellationToken cancellationToken)
    {
        return await store.FirstOrDefaultAsync(p => p.UserId == query.UserId, p => p.UserName, cancellationToken: cancellationToken);
    }
}