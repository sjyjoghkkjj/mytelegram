namespace MyTelegram.Messenger.Services.Impl;

public class ChannelAppService(IQueryProcessor queryProcessor,
    IReadModelCacheHelper<IChannelReadModel> channelReadModelCacheHelper,
    IReadModelCacheHelper<IChannelFullReadModel> channelFullReadModelCacheHelper) : ReadModelWithCacheAppService<IChannelReadModel>(channelReadModelCacheHelper), IChannelAppService, ITransientDependency
{
    public Task<IChannelFullReadModel?> GetChannelFullAsync(long channelId)
    {
        return channelFullReadModelCacheHelper.GetOrCreateAsync(channelId,
            () => queryProcessor.ProcessAsync(new GetChannelFullByIdQuery(channelId)), p => p.Id);
    }

    protected override Task<IChannelReadModel?> GetReadModelAsync(long id)
    {
        return queryProcessor.ProcessAsync(new GetChannelByIdQuery(id));
    }

    protected override string GetReadModelId(IChannelReadModel readModel) => readModel.Id;

    protected override long GetReadModelInt64Id(IChannelReadModel readModel) => readModel.ChannelId;
    protected override Task<IChannelReadModel?> CreateNonExistsReadModelAsync(long id)
    {
        return Task.FromResult<IChannelReadModel?>(null);
    }

    protected override Task<IReadOnlyCollection<IChannelReadModel>> GetReadModelListAsync(List<long> ids)
    {
        return queryProcessor.ProcessAsync(new GetChannelByChannelIdListQuery(ids));
    }
}