namespace MyTelegram.Converters.Responses;

public interface IUpdatesResponseService
{
    IUpdates ToLayeredData(IUpdates latestLayerData, int layer);
    IUpdate ToLayeredData(IUpdate latestLayerData, int layer);
}