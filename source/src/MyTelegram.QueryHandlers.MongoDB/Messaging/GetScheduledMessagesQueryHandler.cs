namespace MyTelegram.QueryHandlers.MongoDB.Messaging;

public class GetScheduledMessagesQueryHandler(IMongoDbReadModelStore<MyTelegram.ReadModel.Impl.ScheduleMessageReadModel> store)
    : IQueryHandler<GetScheduledMessagesQuery, IReadOnlyCollection<IScheduleMessageReadModel>>
{
    public async Task<IReadOnlyCollection<IScheduleMessageReadModel>> ExecuteQueryAsync(GetScheduledMessagesQuery query, CancellationToken cancellationToken)
    {
        var res = await store.FindAsync(p => p.UserId == query.UserId && p.ToPeerId == query.PeerId, cancellationToken: cancellationToken);
        return res.Items.OrderBy(p => p.ScheduleDate).ToList();
    }
}

