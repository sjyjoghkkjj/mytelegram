namespace MyTelegram.Messenger.Services;

public interface IGiftCatalogService : ITransientDependency
{
    Task<IReadOnlyList<MyTelegram.Schema.IStarGift>> GetCatalogAsync(CancellationToken cancellationToken = default);
}

