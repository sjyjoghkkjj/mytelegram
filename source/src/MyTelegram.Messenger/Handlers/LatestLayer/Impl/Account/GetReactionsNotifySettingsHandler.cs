// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Account;

///<summary>
/// See <a href="https://corefork.telegram.org/method/account.getReactionsNotifySettings" />
///</summary>
internal sealed class GetReactionsNotifySettingsHandler : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestGetReactionsNotifySettings, MyTelegram.Schema.IReactionsNotifySettings>,
    Account.IGetReactionsNotifySettingsHandler
{
    protected override Task<MyTelegram.Schema.IReactionsNotifySettings> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestGetReactionsNotifySettings obj)
    {
        return System.Threading.Tasks.Task.FromResult<MyTelegram.Schema.IReactionsNotifySettings>(
            new TReactionsNotifySettings
            {
                ShowPreviews = true,
                Sound = new TNotificationSoundDefault()
            });
    }
}
