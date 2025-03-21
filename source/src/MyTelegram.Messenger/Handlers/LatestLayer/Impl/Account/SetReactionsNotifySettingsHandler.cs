// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Account;

///<summary>
/// See <a href="https://corefork.telegram.org/method/account.setReactionsNotifySettings" />
///</summary>
internal sealed class SetReactionsNotifySettingsHandler : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestSetReactionsNotifySettings, MyTelegram.Schema.IReactionsNotifySettings>,
    Account.ISetReactionsNotifySettingsHandler
{
    protected override Task<MyTelegram.Schema.IReactionsNotifySettings> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestSetReactionsNotifySettings obj)
    {
        throw new NotImplementedException();
    }
}
