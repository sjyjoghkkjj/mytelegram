using EventFlow.ReadStores;

namespace MyTelegram.Messenger.Services.Impl;

public abstract class ReadModelWithCacheAppService<TReadModel>(IReadModelCacheHelper<TReadModel> cacheHelper) : IReadModelWithCacheAppService<TReadModel>
    where TReadModel : IReadModel
{
    public Task<TReadModel?> GetAsync(long? id)
    {
        TReadModel? readModel = default;
        if (!id.HasValue || id == 0)
        {
            return Task.FromResult(readModel);
        }

        return cacheHelper.GetOrCreateAsync(id.Value, () => GetReadModelAsync(id.Value), GetReadModelId);
    }

    public async Task<TReadModel> GetAsync(long id)
    {
        var readModel = await cacheHelper.GetOrCreateAsync(id, () => GetReadModelAsync(id), GetReadModelId);

        return readModel ?? throw new ArgumentException($"ReadModel({typeof(TReadModel).Name}) with id {id} not exists");
    }

    public async Task<IReadOnlyCollection<TReadModel>> GetListAsync(List<long> ids)
    {
        //var readModels = new List<TReadModel>();
        var readModels = new Dictionary<long, TReadModel>();
        var idsNotExistInCache = new HashSet<long>();
        foreach (var id in ids)
        {
            if (cacheHelper.TryGetReadModel(id, out var readModel))
            {
                if (readModel != null)
                {
                    readModels.TryAdd(id, readModel);
                }
                else
                {
                    idsNotExistInCache.Add(id);
                }
            }
            else
            {
                idsNotExistInCache.Add(id);
            }
        }

        if (idsNotExistInCache.Count > 0)
        {
            var dbReadModels = await GetReadModelListAsync([.. idsNotExistInCache]);
            //readModels.AddRange(dbReadModels);
            foreach (var readModel in dbReadModels)
            {
                readModels.TryAdd(GetReadModelInt64Id(readModel), readModel);
                cacheHelper.Add(GetReadModelInt64Id(readModel), GetReadModelId(readModel), readModel);
            }
        }

        return readModels.Values;
    }

    protected abstract Task<IReadOnlyCollection<TReadModel>> GetReadModelListAsync(List<long> ids);

    protected abstract Task<TReadModel?> GetReadModelAsync(long id);
    protected abstract string GetReadModelId(TReadModel readModel);
    protected abstract long GetReadModelInt64Id(TReadModel readModel);
}