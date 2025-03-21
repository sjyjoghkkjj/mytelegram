namespace MyTelegram.QueryHandlers.MongoDB.Channel;

public class GetChannelMembersByChannelIdQueryHandler(IQueryOnlyReadModelStore<ChannelMemberReadModel> store) :
    IQueryHandler<GetChannelMembersByChannelIdQuery,
    IReadOnlyCollection<IChannelMemberReadModel>>
{
    public async Task<IReadOnlyCollection<IChannelMemberReadModel>> ExecuteQueryAsync(
        GetChannelMembersByChannelIdQuery query,
        CancellationToken cancellationToken)
    {
        Expression<Func<ChannelMemberReadModel, bool>> filter = p => !p.Left && p.ChannelId == query.ChannelId;
        filter = filter.WhereIf(query.MemberUserIdList.Count > 0, p => query.MemberUserIdList.Contains(p.UserId));

        return await store.FindAsync(filter, cancellationToken: cancellationToken);
    }
}
