namespace MyTelegram.QueryHandlers.MongoDB.Channel;

public class GetChannelMemberByUidQueryHandler(IMongoDbReadModelStore<ChannelMemberReadModel> store)
    : IQueryHandler<GetChannelMemberByUserIdQuery, IChannelMemberReadModel?>
{
    public async Task<IChannelMemberReadModel?> ExecuteQueryAsync(GetChannelMemberByUserIdQuery query,
        CancellationToken cancellationToken)
    {
        var id = ChannelMemberId.Create(query.ChannelId, query.UserId);
        var item = await store.GetAsync(id.Value, cancellationToken);
        return item.ReadModel;
    }
}
