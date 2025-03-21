using MyTelegram.QueryHandlers.MongoDB;

// ReSharper disable once CheckNamespace
namespace MyTelegram.Queries;
//.MongoDB.User

public class
    SearchUserByKeywordQueryHandler(IQueryOnlyReadModelStore<UserReadModel> store) : IQueryHandler<SearchUserByKeywordQuery, IReadOnlyCollection<IUserReadModel>>
{
    public async Task<IReadOnlyCollection<IUserReadModel>> ExecuteQueryAsync(SearchUserByKeywordQuery query,
        CancellationToken cancellationToken)
    {
        var q = query.Keyword;
        if (!string.IsNullOrEmpty(q) && q.StartsWith("@"))
        {
            q = query.Keyword[1..];
        }

        Expression<Func<UserReadModel, bool>> predicate = x => true;
        predicate = predicate.WhereIf(!string.IsNullOrEmpty(q),
            p => (p.UserName != null && p.UserName.StartsWith(q)) || p.FirstName.Contains(q));

        return await store.FindAsync(predicate, 0, 50, new SortOptions<UserReadModel>(p => p.UserName, SortType.Ascending), cancellationToken);
    }
}
