// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Account;

///<summary>
/// Get autosave settings
/// See <a href="https://corefork.telegram.org/method/account.getAutoSaveSettings" />
///</summary>
internal sealed class GetAutoSaveSettingsHandler : RpcResultObjectHandler<MyTelegram.Schema.Account.RequestGetAutoSaveSettings, MyTelegram.Schema.Account.IAutoSaveSettings>,
    Account.IGetAutoSaveSettingsHandler
{
    protected override Task<MyTelegram.Schema.Account.IAutoSaveSettings> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Account.RequestGetAutoSaveSettings obj)
    {
        throw new NotImplementedException();
    }
}
