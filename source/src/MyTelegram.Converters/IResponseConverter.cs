namespace MyTelegram.Converters;

public interface IResponseConverter : ILayeredConverter
{
}

public interface IRequestConverter<in TOldLayerRequestData, out TNewLayerRequestData> : IRequestConverter
{
    TNewLayerRequestData ToLatestLayerData(IRequestInput request, TOldLayerRequestData data);
}

public interface IResponseConverter<in TLatestResponseData, out TOldLayerResponseData> : IResponseConverter
{
    TOldLayerResponseData ToLayeredData(TLatestResponseData data);
    //TOldLayerResponseData ToLatestLayerData(TOldLayerResponseData oldLayerData);
}