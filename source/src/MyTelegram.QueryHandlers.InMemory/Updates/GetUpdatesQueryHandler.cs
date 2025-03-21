namespace MyTelegram.QueryHandlers.InMemory.Updates;

public class GetUpdatesQueryHandler(IQueryOnlyReadModelStore<UpdatesReadModel> store) : IQueryHandler<GetUpdatesQuery, IReadOnlyCollection<IUpdatesReadModel>>
{
    public async Task<IReadOnlyCollection<IUpdatesReadModel>> ExecuteQueryAsync(GetUpdatesQuery query,
        CancellationToken cancellationToken)
    {
        Expression<Func<UpdatesReadModel, bool>> predicate = p => (p.OwnerPeerId == query.PeerId) /*&& (p.OnlySendToUserId == null || p.OnlySendToUserId == query.SelfUserId) */&& p.Pts > query.MinPts;
        if (query.Date > 0)
        {
            predicate = predicate.And(p => p.Date > query.Date);
        }

        return await store.FindAsync(predicate,
            0,
            query.Limit,
            cancellationToken: cancellationToken);
    }
}