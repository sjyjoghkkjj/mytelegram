namespace MyTelegram.QueryHandlers.MongoDB.Channel;

public class GetSendAsQueryHandler
    (IQueryOnlyReadModelStore<ChannelReadModel> store) : IQueryHandler<GetSendAsQuery, IReadOnlyCollection<IChannelReadModel>>
{
    public async Task<IReadOnlyCollection<IChannelReadModel>> ExecuteQueryAsync(GetSendAsQuery query, CancellationToken cancellationToken)
    {
        return await store.FindAsync(p =>
            p.ChannelId == query.LinkedChannelId || p.LinkedChatId == query.LinkedChannelId, cancellationToken: cancellationToken);
        //return await store.FirstOrDefaultAsync(p => p.LinkedChatId == query.LinkedChannelId, cancellationToken: cancellationToken);
    }
}