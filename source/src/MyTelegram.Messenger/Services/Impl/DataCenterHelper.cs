namespace MyTelegram.Messenger.Services.Impl;

public class DataCenterHelper(IOptions<MyTelegramMessengerServerOptions> options)
    : IDataCenterHelper, ITransientDependency
{
    public int GetMediaDcId()
    {
        //var dcId=_options.Value.IsMediaDc
        var defaultDcId = MyTelegramConsts.MediaDcId;
        var dc = options.Value.DcOptions?.FirstOrDefault(p => p.Id == defaultDcId);
        if (dc != null)
        {
            return defaultDcId;
        }

        return options.Value.ThisDcId;
    }

    public bool IsCdnDc(int dcId)
    {
        return options.Value.DcOptions?.Any(d => d.Id == dcId && d.Cdn) == true;
    }

    public int GetThisDcId()
    {
        return options.Value.ThisDcId;
    }
}