namespace MyTelegram.Messenger.Services.Impl;
public partial class AppConfigHelper : IAppConfigHelper, ISingletonDependency
{
    public int GetAppConfigHash()
    {
        return _hash;
    }
}