using EventFlow.ReadStores;

namespace MyTelegram.Messenger.Services.Interfaces;

public interface IReadModelWithCacheAppService<TReadModel>
    where TReadModel : IReadModel
{
    Task<TReadModel?> GetAsync(long? id);
    Task<TReadModel> GetAsync(long id);

    Task<IReadOnlyCollection<TReadModel>> GetListAsync(IEnumerable<long> ids);
}