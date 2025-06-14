namespace MyTelegram.Converters.Responses;

public interface IUpdatesResponseService
{
    IUpdates ToLayeredData(long userId, long accessHashKeyId, IUpdates latestLayerData, int layer);
    IUpdate ToLayeredData(long userId, long accessHashKeyId, IUpdate latestLayerData, int layer);
}