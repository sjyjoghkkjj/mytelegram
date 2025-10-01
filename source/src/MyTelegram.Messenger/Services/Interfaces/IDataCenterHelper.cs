namespace MyTelegram.Messenger.Services.Interfaces;

public interface IDataCenterHelper
{
    int GetMediaDcId();
    bool IsCdnDc(int dcId);
    int GetThisDcId();
    int? GetFirstCdnDcId();
}